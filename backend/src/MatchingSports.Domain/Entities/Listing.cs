using MatchingSports.Domain.Enums;
using MatchingSports.Domain.ValueObjects;
using MatchingSports.Shared;

namespace MatchingSports.Domain.Entities;

public sealed class Listing : ConcurrentAuditableEntity
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid? HostTeamId { get; set; }
    public Guid SportId { get; set; }
    public Guid FormatId { get; set; }
    public ListingMode Mode { get; set; }
    public string? Description { get; private set; }
    public string Title { get; set; } = null!;
    public string? Note { get; set; }
    public SportLevel LevelMin { get; set; } = SportLevel.BEGINNER;
    public SportLevel LevelMax { get; set; } = SportLevel.ADVANCED;
    public short OpenSlots { get; set; }
    public short AcceptedSlots { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public short DurationMinutes { get; set; }
    public DateTimeOffset CutoffAt { get; set; }
    public string AddressText { get; set; } = null!;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public ListingStatus Status { get; set; } = ListingStatus.OPEN;
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public User OwnerUser { get; set; } = null!;
    public Team? HostTeam { get; set; }
    public Sport Sport { get; set; } = null!;
    public SportFormat Format { get; set; } = null!;
    public ICollection<ListingRequest> Requests { get; set; } = [];
    public Match? Match { get; set; }

    public static Listing Create(Guid ownerUserId, ListingValues values, DateTimeOffset now)
    {
        MatchingRules.Require(ownerUserId != Guid.Empty, "Người đăng kèo không hợp lệ.");
        var listing = new Listing { Id = Guid.NewGuid(), OwnerUserId = ownerUserId };
        listing.ApplyValues(values, now);
        return listing;
    }

    public bool IsAvailable(DateTimeOffset now) => Status == ListingStatus.OPEN && DeletedAt is null
        && AcceptedSlots < OpenSlots && now < CutoffAt && now < StartAt;

    public void UpdateDetails(ListingValues values, DateTimeOffset now)
    {
        MatchingRules.Require(IsAvailable(now), "Chỉ kèo đang mở còn hiệu lực được sửa.");
        MatchingRules.Require(AcceptedSlots == 0, "Không được sửa thông tin khi đã có người được duyệt.");
        MatchingRules.Require(values.SportId == SportId && values.FormatId == FormatId && values.HostTeamId == HostTeamId && values.Mode == Mode,
            "Không được đổi môn, thể thức, đội chủ nhà hoặc chế độ của kèo.");
        ApplyValues(values, now);
        Match?.RefreshSchedule(this);
        RenewConcurrencyToken();
    }

    private void ApplyValues(ListingValues values, DateTimeOffset now)
    {
        values.Validate(now);
        Description = ResourceDescription.Normalize(values.Description);
        SportId = values.SportId; FormatId = values.FormatId; HostTeamId = values.HostTeamId; Mode = values.Mode;
        Title = values.Title.Trim(); Note = StringNormalizer.StartEndSpaceTrim(values.Note);
        LevelMin = values.LevelMin; LevelMax = values.LevelMax; OpenSlots = values.OpenSlots;
        StartAt = values.StartAt.ToUniversalTime(); CutoffAt = values.CutoffAt.ToUniversalTime(); DurationMinutes = values.DurationMinutes;
        AddressText = values.AddressText.Trim(); Latitude = decimal.Round(values.Latitude, 6); Longitude = decimal.Round(values.Longitude, 6);
    }

    public ListingRequest RequestToJoin(Guid? userId, Guid? teamId, string? message, DateTimeOffset now)
    {
        MatchingRules.Require(IsAvailable(now), "Kèo đã hết hạn hoặc không còn chỗ.");
        MatchingRules.Require(Mode == ListingMode.FIND_PLAYERS
            ? userId is not null && userId != Guid.Empty && teamId is null && userId != OwnerUserId
            : teamId is not null && teamId != Guid.Empty && userId is null && teamId != HostTeamId,
            "Người hoặc đội đăng ký không phù hợp với kèo.");
        MatchingRules.Require(!Requests.Any(request => request.Status is RequestStatus.PENDING or RequestStatus.ACCEPTED
            && (userId is not null ? request.ApplicantUserId == userId : request.ApplicantTeamId == teamId)),
            "Đã có yêu cầu đang chờ hoặc đã được duyệt.", ErrorCodes.DuplicateResource);
        var request = ListingRequest.Create(Id, userId, teamId, message);
        Requests.Add(request); RenewConcurrencyToken();
        return request;
    }

    public void AcceptRequest(ListingRequest request, Guid actorId, DateTimeOffset now)
    {
        MatchingRules.Require(IsAvailable(now), "Kèo đã hết hạn hoặc không còn chỗ.");
        RequireRequest(request); request.RequirePending();
        MatchingRules.Require(request.RequestedSlots == 1 && AcceptedSlots + request.RequestedSlots <= OpenSlots, "Kèo không còn đủ chỗ.");
        Match ??= MatchingSports.Domain.Entities.Match.Create(this);
        Match.AddApplicant(request);
        request.Accept(actorId, now);
        AcceptedSlots++;
        if (AcceptedSlots == OpenSlots) Status = ListingStatus.MATCHED;
        RenewConcurrencyToken();
    }

    public void RejectRequest(ListingRequest request, Guid actorId, string reason, DateTimeOffset now)
    {
        RequireRequest(request);
        MatchingRules.Require(Status is ListingStatus.OPEN or ListingStatus.MATCHED && now < StartAt, "Kèo không còn nhận xử lý yêu cầu.");
        request.Reject(actorId, reason, now); RenewConcurrencyToken();
    }

    public void WithdrawRequest(ListingRequest request, DateTimeOffset now)
    {
        RequireRequest(request);
        MatchingRules.Require(request.Status is RequestStatus.PENDING or RequestStatus.ACCEPTED, "Yêu cầu không còn được rút.");
        if (request.Status == RequestStatus.ACCEPTED)
        {
            MatchingRules.Require(Status is ListingStatus.OPEN or ListingStatus.MATCHED && now < CutoffAt && now < StartAt
                && Match?.Status == MatchStatus.PENDING, "Chỉ được rút chỗ đã duyệt trước xác nhận và trước hạn đăng ký.");
            Match!.WithdrawApplicant(request);
            AcceptedSlots--; Status = ListingStatus.OPEN;
        }
        request.Withdraw(now); RenewConcurrencyToken();
    }

    public void ConfirmMatch(DateTimeOffset now)
    {
        MatchingRules.Require(Status == ListingStatus.MATCHED && AcceptedSlots == OpenSlots && now < StartAt && Match is not null,
            "Trận phải đủ chỗ và chưa bắt đầu để xác nhận.");
        Match!.Confirm(now); Status = ListingStatus.CONFIRMED;
        foreach (var request in Requests.Where(request => request.Status == RequestStatus.PENDING)) request.Expire(now);
        RenewConcurrencyToken();
    }

    public bool Cancel(Guid? actorId, string reason, DateTimeOffset now, bool administrative = false)
    {
        if (Status == ListingStatus.CANCELLED) return false;
        MatchingRules.Require(Status is ListingStatus.OPEN or ListingStatus.MATCHED or ListingStatus.CONFIRMED,
            "Kèo đã kết thúc không được hủy.");
        MatchingRules.Require(administrative || now < StartAt, "Không được hủy sau giờ bắt đầu trận.");
        var normalizedReason = MatchingRules.Text(reason, 1000, "Lý do hủy");
        Match?.Cancel(actorId, normalizedReason, now);
        Status = ListingStatus.CANCELLED; CancelledAt = now; CancelReason = normalizedReason;
        foreach (var request in Requests.Where(request => request.Status is RequestStatus.PENDING or RequestStatus.ACCEPTED)) request.Cancel(now);
        RenewConcurrencyToken(); return true;
    }

    public bool Expire(DateTimeOffset now)
    {
        if (!((Status == ListingStatus.OPEN && now >= CutoffAt) || (Status == ListingStatus.MATCHED && now >= StartAt))) return false;
        Match?.Cancel(null, "Kèo hết hạn trước khi xác nhận.", now);
        Status = ListingStatus.EXPIRED;
        foreach (var request in Requests.Where(request => request.Status is RequestStatus.PENDING or RequestStatus.ACCEPTED)) request.Expire(now);
        RenewConcurrencyToken(); return true;
    }

    public void CompleteMatch(DateTimeOffset now)
    {
        MatchingRules.Require(Status == ListingStatus.CONFIRMED && Match is not null && now >= StartAt.AddMinutes(DurationMinutes),
            "Chỉ được hoàn tất trận đã xác nhận sau thời gian kết thúc.");
        Match!.Complete(now); Status = ListingStatus.COMPLETED; RenewConcurrencyToken();
    }

    private void RequireRequest(ListingRequest request) =>
        MatchingRules.Require(request.ListingId == Id && Requests.Contains(request), "Yêu cầu không thuộc kèo.");
}
