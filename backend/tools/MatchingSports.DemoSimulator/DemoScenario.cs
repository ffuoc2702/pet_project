using MatchingSports.Application.Features.Matching;
using MatchingSports.Application.Features.Notification;
using MatchingSports.Application.Features.PublicProfile;
using MatchingSports.Application.Features.Team;
using MatchingSports.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MatchingSports.DemoSimulator;

internal sealed class DemoScenario(DemoSession session, DemoState state, DemoCheckpoint checkpoint, Guid sportId, Guid formatId)
{
    private Guid User(int index) => state.Resources[$"user-{index}"];
    private Guid Free(int offset) => User(state.TeamCount * 5 + offset);
    private string Prefix => $"[demo:{state.RunId}]";

    public async Task RunAsync()
    {
        await StepAsync("teams-and-deputies", CreateTeamsAsync);
        await StepAsync("listings", async () =>
        {
            for (var index = 0; index < state.ListingCount; index++)
                if (index != 7) await EnsureListingAsync(index);
        });
        await StepAsync("radius-and-time-search", async () =>
        {
            var results = await session.SendAsync(Free(0), new GetListingsQuery
            {
                ActorUserId = Free(0),
                SportId = sportId,
                Mode = ListingMode.FIND_PLAYERS,
                SearchString = Prefix,
                Latitude = 10.8m,
                Longitude = 106.7m,
                RadiusMeters = 2000,
                StartFrom = state.AnchorAt,
                StartTo = state.AnchorAt.AddDays(7),
                PageSize = 100
            });
            Require(results.Items.Any(item => item.Id == state.Resources["listing-0"]), "Không tìm thấy kèo demo gần vị trí.");
            Require(results.Items.All(item => item.DistanceMeters <= 2000), "Filter bán kính không đúng.");
        });
        await StepAsync("player-confirmed", () => PlayAsync(0, [Free(2), Free(3)], null, false));
        await StepAsync("player-cancelled", () => PlayAsync(1, [Free(2)], null, true));
        await StepAsync("team-confirmed", () => PlayAsync(2, [User(5)], state.Resources["team-1"], false));
        await StepAsync("team-cancelled", () => PlayAsync(3, [User(0)], state.Resources["team-0"], true));
        await StepAsync("reject-and-withdraw", RejectAndWithdrawAsync);
        await StepAsync("listing-cancelled", async () =>
        {
            var listing = await ListingAsync(6);
            if (listing.Status == ListingStatus.CANCELLED) return;
            _ = await EnsureRequestAsync(6, Free(2), null);
            listing = await ListingAsync(6);
            await session.SendAsync(Free(1), new CancelListingCommand(Free(1), listing.Id, listing.ConcurrencyToken, "Sân demo đổi lịch."));
        });
        await StepAsync("match-reminder", async () =>
        {
            await PlayAsync(5, [Free(2)], null, false);
            await session.PublishRemindersAsync();
            var matchId = (await ListingAsync(5)).MatchId!.Value;
            Require(await session.ReadAsync((db, ct) => db.NotificationHistories.AnyAsync(history =>
                history.Type == NotificationType.MATCH_REMINDER && history.EntityId == matchId && history.UserId == Free(2), ct)),
                "Chưa tạo được nhắc lịch demo; lịch đã quá hạn hoặc worker chưa sẵn sàng.");
        });
        await StepAsync("watch-before-new-listing", CreateWatchAsync);
        await StepAsync("watch-matched-and-deduplicated", async () =>
        {
            var target = await EnsureListingAsync(7);
            await session.PumpMatchingAsync();
            var count = await session.ReadAsync((db, ct) => db.ListingWatchDeliveries.CountAsync(delivery =>
                delivery.ListingId == target && delivery.UserId == Free(0), ct));
            Require(count == 1, "Worker chưa tạo delivery duy nhất cho watch demo.");
            await session.PumpMatchingAsync();
            Require(await session.ReadAsync((db, ct) => db.ListingWatchDeliveries.CountAsync(delivery =>
                delivery.ListingId == target && delivery.UserId == Free(0), ct)) == 1, "Watch bị tạo delivery trùng.");
        });
        await StepAsync("public-profiles-and-inbox", async () =>
        {
            _ = await session.SendAsync(Free(0), new GetPublicUserProfileQuery(Free(0), Free(2)));
            var team = await session.SendAsync(Free(0), new GetPublicTeamProfileQuery(Free(0), state.Resources["team-0"]));
            Require(team.ActiveMemberCount == 5, "Team demo không đủ 5 thành viên.");
            var inbox = await session.SendAsync(Free(2), new GetNotificationsQuery { ActorUserId = Free(2), PageSize = 20 });
            Require(inbox.TotalItems > 0, "Inbox demo chưa có thông báo.");
        });
    }

    private async Task CreateTeamsAsync()
    {
        for (var index = 0; index < state.TeamCount; index++)
        {
            var captain = User(index * 5);
            var name = $"{Prefix} Team {index + 1:00}";
            var id = await session.ReadAsync((db, ct) => db.Teams.AsNoTracking()
                .Where(team => team.Name == name && team.CaptainUserId == captain).Select(team => (Guid?)team.Id).SingleOrDefaultAsync(ct));
            if (state.Resources.TryGetValue($"team-{index}", out var savedTeam) && id != savedTeam)
                throw new DemoSimulationException("Team checkpoint đã bị xoá/thay đổi; dùng RunId mới.");
            id ??= (await session.SendAsync(captain, new CreateTeamCommand(captain, sportId, name,
                "Đội bóng giả lập cho portfolio.", "TP.HCM", null))).Id;
            state.Resources[$"team-{index}"] = id.Value;
            await checkpoint.SaveAsync(state, session.Token);
            for (var member = 1; member < 5; member++)
            {
                var userId = User(index * 5 + member);
                if (await session.ReadAsync((db, ct) => db.TeamMembers.AnyAsync(link => link.TeamId == id && link.UserId == userId && link.Status == MemberStatus.ACTIVE, ct)))
                    continue;
                var invitationId = await session.ReadAsync((db, ct) => db.TeamInvitations.AsNoTracking()
                    .Where(item => item.TeamId == id && item.UserId == userId && item.Status == TeamRequestStatus.PENDING)
                    .Select(item => (Guid?)item.Id).SingleOrDefaultAsync(ct));
                var invitation = invitationId is null
                    ? await session.SendAsync(captain, new InviteTeamMemberCommand(captain, id.Value, userId))
                    : await session.ReadAsync((db, ct) => db.TeamInvitations.AsNoTracking().Where(item => item.Id == invitationId.Value).Select(item => new TeamInvitationDto { Id = item.Id, ConcurrencyToken = item.ConcurrencyToken }).SingleAsync(ct));
                await session.SendAsync(userId, new AcceptTeamInvitationCommand(userId, id.Value, invitation.Id, invitation.ConcurrencyToken));
            }
            var deputy = User(index * 5 + 1);
            var alreadyDeputy = await session.ReadAsync((db, ct) => db.TeamMembers.AnyAsync(member =>
                member.TeamId == id && member.UserId == deputy && member.Role == TeamRole.DEPUTY && member.Status == MemberStatus.ACTIVE, ct));
            if (!alreadyDeputy)
            {
                var team = await session.SendAsync(captain, new GetTeamQuery(captain, id.Value));
                await session.SendAsync(captain, new AppointTeamDeputyCommand(captain, id.Value, deputy, team.ConcurrencyToken));
            }
        }
    }

    private Guid Host(int index) => index == 2 ? User(0) : index == 3 ? User(5) : Free(1);
    private async Task<Guid> EnsureListingAsync(int index)
    {
        var key = $"listing-{index}";
        var title = $"{Prefix} Kèo {index + 1:00}";
        var host = Host(index);
        var existing = await session.ReadAsync((db, ct) => db.Listings.AsNoTracking()
            .Where(listing => listing.Title == title && listing.OwnerUserId == host).Select(listing => (Guid?)listing.Id).SingleOrDefaultAsync(ct));
        if (state.Resources.TryGetValue(key, out var saved) && existing != saved)
            throw new DemoSimulationException("Kèo checkpoint đã bị xoá/thay đổi; dùng RunId mới.");
        if (existing is not null) { state.Resources[key] = existing.Value; return existing.Value; }
        if (!state.Schedules.TryGetValue(key, out var startAt))
        {
            var lead = await session.ReadAsync((db, ct) => db.SystemSettings.Select(settings => settings.MatchReminderLeadMinutes).SingleAsync(ct));
            startAt = index == 5 ? DateTimeOffset.UtcNow.AddMinutes(Math.Max(0.5, lead * 0.75))
                : index == 7 ? state.AnchorAt.AddHours(72) : state.AnchorAt.AddHours(24 + index);
            state.Schedules[key] = startAt;
            await checkpoint.SaveAsync(state, session.Token);
        }
        var cutoff = index == 5 ? DateTimeOffset.UtcNow.AddTicks((startAt - DateTimeOffset.UtcNow).Ticks / 2) : startAt.AddHours(-1);
        var teamId = index == 2 ? state.Resources["team-0"] : index == 3 ? state.Resources["team-1"] : (Guid?)null;
        var mode = teamId is null ? ListingMode.FIND_PLAYERS : ListingMode.FIND_OPPONENT;
        var far = index > 7 && index % 3 == 2;
        var input = new ListingInput(sportId, formatId, teamId, mode, title, "Kèo demo để kiểm tra luồng đăng ký.",
            SportLevel.BEGINNER, SportLevel.ADVANCED, index is 0 or 4 ? (short)2 : (short)1, startAt, 90, cutoff,
            far ? "Sân demo Thủ Đức" : "Sân demo Bình Thạnh", far ? 10.87m : 10.8m, far ? 106.8m : 106.7m);
        var result = await session.SendAsync(host, new CreateListingCommand(host, input));
        state.Resources[key] = result.Id;
        await checkpoint.SaveAsync(state, session.Token);
        return result.Id;
    }

    private Task<ListingDto> ListingAsync(int index) =>
        session.SendAsync(Host(index), new GetListingQuery(Host(index), state.Resources[$"listing-{index}"]));

    private async Task<ListingRequestDto> EnsureRequestAsync(int index, Guid applicant, Guid? teamId)
    {
        var listingId = state.Resources[$"listing-{index}"];
        var id = await session.ReadAsync((db, ct) => db.ListingRequests.AsNoTracking()
            .Where(request => request.ListingId == listingId && (teamId == null
                ? request.ApplicantUserId == applicant : request.ApplicantTeamId == teamId))
            .OrderByDescending(request => request.CreatedAt).Select(request => (Guid?)request.Id).FirstOrDefaultAsync(ct));
        return id is null ? await session.SendAsync(applicant, new CreateListingRequestCommand(applicant, listingId, teamId, "Đăng ký demo"))
            : await session.SendAsync(applicant, new GetListingRequestQuery(applicant, listingId, id.Value));
    }

    private async Task PlayAsync(int index, Guid[] applicants, Guid? teamId, bool cancel)
    {
        var listing = await ListingAsync(index);
        if (listing.Status is ListingStatus.CONFIRMED or ListingStatus.CANCELLED)
        {
            if (!cancel)
            {
                Require(listing.Status == ListingStatus.CONFIRMED, "Trận demo đã bị huỷ ngoài kịch bản.");
                return;
            }
            if (listing.Status == ListingStatus.CANCELLED) return;
        }
        else
        {
            foreach (var actor in applicants)
            {
                var request = await EnsureRequestAsync(index, actor, teamId);
                if (request.Status == RequestStatus.PENDING)
                    await session.SendAsync(Host(index), new AcceptListingRequestCommand(Host(index), listing.Id, request.Id, request.ConcurrencyToken));
                else Require(request.Status == RequestStatus.ACCEPTED, "Request demo đã chuyển trạng thái ngoài kịch bản.");
            }
            listing = await ListingAsync(index);
            var match = await session.SendAsync(Host(index), new GetMatchQuery(Host(index), listing.MatchId!.Value));
            if (match.Status == MatchStatus.PENDING)
                await session.SendAsync(Host(index), new ConfirmMatchCommand(Host(index), match.Id, match.ConcurrencyToken));
        }
        if (cancel)
        {
            listing = await ListingAsync(index);
            var match = await session.SendAsync(Host(index), new GetMatchQuery(Host(index), listing.MatchId!.Value));
            await session.SendAsync(Host(index), new CancelMatchCommand(Host(index), match.Id, match.ConcurrencyToken, "Mưa giả lập; huỷ trận demo."));
        }
    }

    private async Task RejectAndWithdrawAsync()
    {
        var rejected = await EnsureRequestAsync(4, Free(4), null);
        if (rejected.Status == RequestStatus.PENDING)
            await session.SendAsync(Host(4), new RejectListingRequestCommand(Host(4), rejected.ListingId, rejected.Id, rejected.ConcurrencyToken, "Đã có lựa chọn khác trong kịch bản demo."));
        else Require(rejected.Status == RequestStatus.REJECTED, "Request demo không còn ở trạng thái từ chối.");
        var withdrawn = await EnsureRequestAsync(4, Free(5), null);
        if (withdrawn.Status == RequestStatus.PENDING)
            await session.SendAsync(Free(5), new WithdrawListingRequestCommand(Free(5), withdrawn.ListingId, withdrawn.Id, withdrawn.ConcurrencyToken));
        else Require(withdrawn.Status == RequestStatus.WITHDRAWN, "Request demo không còn ở trạng thái rút đăng ký.");
    }

    private async Task CreateWatchAsync()
    {
        var time = state.AnchorAt.AddHours(72);
        var criteria = new ListingWatchInput(sportId, formatId, 10.8m, 106.7m, 2000, time.AddMinutes(-1), time.AddMinutes(1));
        var existing = await session.ReadAsync((db, ct) => db.ListingWatches.AsNoTracking()
            .Where(watch => watch.UserId == Free(0) && watch.StartFrom == criteria.StartFrom && watch.StartTo == criteria.StartTo)
            .Select(watch => (Guid?)watch.Id).SingleOrDefaultAsync(ct));
        if (existing is null)
        {
            var before = await session.SendAsync(Free(0), new GetListingsQuery
            {
                ActorUserId = Free(0),
                SportId = sportId,
                FormatId = formatId,
                Mode = ListingMode.FIND_PLAYERS,
                Latitude = criteria.Latitude,
                Longitude = criteria.Longitude,
                RadiusMeters = criteria.RadiusMeters,
                StartFrom = criteria.StartFrom,
                StartTo = criteria.StartTo,
                PageSize = 20
            });
            Require(before.TotalItems == 0, "Đã có kèo trong cửa sổ watch; dùng RunId mới hoặc database demo riêng.");
            existing = (await session.SendAsync(Free(0), new CreateListingWatchCommand(Free(0), criteria, false))).Id;
        }
        state.Resources["watch"] = existing.Value;
        await checkpoint.SaveAsync(state, session.Token);
    }

    private async Task StepAsync(string name, Func<Task> action)
    {
        if (state.CompletedSteps.Contains(name)) return;
        state.ActiveStep = name;
        await checkpoint.SaveAsync(state, session.Token);
        await action();
        state.CompletedSteps.Add(name);
        await checkpoint.SaveAsync(state, session.Token);
        Console.WriteLine($"Đạt: {name}");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new DemoSimulationException(message);
    }
}
