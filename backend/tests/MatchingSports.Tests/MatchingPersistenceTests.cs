using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MatchingSports.Api.Contracts;
using MatchingSports.Api.Controllers;
using MatchingSports.Api.ErrorHandling;
using MatchingSports.Api.Identity;
using MatchingSports.Application;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Matching;
using MatchingSports.Application.Features.Notification;
using MatchingSports.Application.Features.PublicProfile;
using MatchingSports.Application.Features.Team;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Infrastructure;
using MatchingSports.Infrastructure.Persistence;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace MatchingSports.Tests;

public sealed class MatchingPersistenceTests
{
    [RbacDatabaseFact]
    public async Task PlayerWorkflowKeepsVersionsAuditHistoryAndContactOwnership()
    {
        await using var fixture = await MatchingFixture.CreateAsync();
        var host = fixture.Users[0]; var player = fixture.Users[1]; var outsider = fixture.Users[2];
        var listing = await fixture.SendAsync(new CreateListingCommand(host, fixture.Input()));
        var first = await fixture.SendAsync(new CreateListingRequestCommand(player, listing.Id, null, "Tôi muốn đá vãng lai"));
        var pending = await fixture.SendAsync(new CreateListingRequestCommand(outsider, listing.Id, null, null));
        Assert.Equal(player, first.CreatedBy); Assert.NotEqual(Guid.Empty, first.ConcurrencyToken);
        await Assert.ThrowsAsync<ConflictException>(() => fixture.SendAsync(new CreateListingRequestCommand(player, listing.Id, null, null)));
        await Assert.ThrowsAsync<ForbiddenException>(() => fixture.SendAsync(new AcceptListingRequestCommand(outsider, listing.Id, first.Id, first.ConcurrencyToken)));
        await Assert.ThrowsAsync<ForbiddenException>(() => fixture.SendAsync(new GetListingRequestQuery(outsider, listing.Id, first.Id)));
        var accepted = await fixture.SendAsync(new AcceptListingRequestCommand(host, listing.Id, first.Id, first.ConcurrencyToken));
        Assert.Equal(first.CreatedAt, accepted.CreatedAt); Assert.Equal(player, accepted.CreatedBy); Assert.Equal(host, accepted.UpdatedBy);
        listing = await fixture.SendAsync(new GetListingQuery(host, listing.Id));
        Assert.Equal(ListingStatus.MATCHED, listing.Status); Assert.NotNull(listing.MatchId);
        await Assert.ThrowsAsync<ForbiddenException>(() => fixture.SendAsync(new GetMatchContactsQuery(player, listing.MatchId!.Value)));
        var withdrawn = await fixture.SendAsync(new WithdrawListingRequestCommand(player, listing.Id, first.Id, accepted.ConcurrencyToken));
        Assert.Equal(RequestStatus.WITHDRAWN, withdrawn.Status);
        var next = await fixture.SendAsync(new CreateListingRequestCommand(player, listing.Id, null, null));
        accepted = await fixture.SendAsync(new AcceptListingRequestCommand(host, listing.Id, next.Id, next.ConcurrencyToken));
        var match = await fixture.SendAsync(new GetMatchQuery(host, listing.MatchId!.Value));
        Assert.Equal(2, match.Participants.Count);
        await Assert.ThrowsAsync<ConflictException>(() => fixture.SendAsync(new ConfirmMatchCommand(host, match.Id, Guid.NewGuid())));
        match = await fixture.SendAsync(new ConfirmMatchCommand(host, match.Id, match.ConcurrencyToken));
        Assert.Equal(MatchStatus.CONFIRMED, match.Status);
        var contacts = await fixture.SendAsync(new GetMatchContactsQuery(player, match.Id));
        Assert.Equal(2, contacts.Contacts.Count);
        await Assert.ThrowsAsync<ForbiddenException>(() => fixture.SendAsync(new GetMatchContactsQuery(outsider, match.Id)));
        await Assert.ThrowsAsync<ConflictException>(() => fixture.SendAsync(new WithdrawListingRequestCommand(player, listing.Id, accepted.Id, accepted.ConcurrencyToken)));
        Assert.Equal(RequestStatus.EXPIRED, (await fixture.SendAsync(new GetListingRequestQuery(outsider, listing.Id, pending.Id))).Status);
        await Assert.ThrowsAsync<ForbiddenException>(() => fixture.SendAsync(new CancelMatchCommand(player, match.Id, match.ConcurrencyToken, "Mưa")));
        var cancelled = await fixture.SendAsync(new CancelMatchCommand(host, match.Id, match.ConcurrencyToken, "  Mưa lớn  "));
        var before = await fixture.NotificationCountAsync();
        _ = await fixture.SendAsync(new CancelMatchCommand(host, match.Id, match.ConcurrencyToken, "Mưa"));
        Assert.Equal(before, await fixture.NotificationCountAsync());
        Assert.Equal("Mưa lớn", cancelled.CancelReason);
        Assert.All(cancelled.Participants, participant => Assert.Equal(ParticipantStatus.CANCELLED, participant.Status));
        Assert.Equal(ListingStatus.CANCELLED, (await fixture.SendAsync(new GetListingQuery(host, listing.Id))).Status);
    }

    [RbacDatabaseFact]
    public async Task RadiusAndStartTimeSearchAndOverlappingWatchesProduceOneNotificationAndEmail()
    {
        await using var fixture = await MatchingFixture.CreateAsync();
        var host = fixture.Users[0]; var player = fixture.Users[1]; var input = fixture.Input();
        var criteria = fixture.WatchInput();
        _ = await fixture.SendAsync(new CreateListingWatchCommand(player, criteria, false));
        _ = await fixture.SendAsync(new CreateListingWatchCommand(player, criteria with { FormatId = null }, true));
        var near = await fixture.SendAsync(new CreateListingCommand(host, input));
        _ = await fixture.SendAsync(new CreateListingCommand(host, input with { Latitude = 11.8m }));
        _ = await fixture.SendAsync(new CreateListingCommand(host, input with { StartAt = input.StartAt.AddDays(2), CutoffAt = input.CutoffAt.AddDays(2) }));
        _ = await fixture.SendAsync(new CreateListingCommand(host, input with { SportId = fixture.OtherSportId, FormatId = fixture.OtherFormatId }));
        var query = new GetListingsQuery
        {
            ActorUserId = player,
            SportId = fixture.SportId,
            Mode = ListingMode.FIND_PLAYERS,
            StartFrom = input.StartAt,
            StartTo = input.StartAt,
            Latitude = criteria.Latitude,
            Longitude = criteria.Longitude,
            RadiusMeters = 2000,
            FromDate = near.UpdatedAt,
            ToDate = near.UpdatedAt,
            PageSize = 1
        };
        var page = await fixture.SendAsync(query);
        Assert.Equal(near.Id, Assert.Single(page.Items).Id); Assert.Equal(1, page.TotalItems);
        Assert.InRange(page.Items[0].DistanceMeters!.Value, 0, 0.1);
        Assert.Empty((await fixture.SendAsync(query with { PageNumber = 2 })).Items);
        var offsetQuery = query with
        {
            Level = SportLevel.INTERMEDIATE,
            StartFrom = input.StartAt.ToOffset(TimeSpan.FromHours(7)),
            StartTo = input.StartAt.ToOffset(TimeSpan.FromHours(7)),
            FromDate = near.UpdatedAt.ToOffset(TimeSpan.FromHours(7)),
            ToDate = near.UpdatedAt.ToOffset(TimeSpan.FromHours(7))
        };
        Assert.Equal(near.Id, Assert.Single((await fixture.SendAsync(offsetQuery)).Items).Id);
        await fixture.PumpAsync();
        await using (var scope = fixture.Provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
            var history = Assert.Single(await db.NotificationHistories.Where(item => item.Type == NotificationType.LISTING_WATCH_MATCHED).ToListAsync());
            Assert.Equal(player, history.UserId); Assert.Equal(near.Id, history.EntityId); Assert.Null(history.CreatedBy);
            Assert.Equal(EntityTypes.Listing, history.EntityType); Assert.Equal(NotificationDeliveryStatus.PENDING, history.EmailStatus);
            Assert.Single(await db.ListingWatchDeliveries.ToListAsync());
            using var metadata = JsonDocument.Parse(history.PayloadJson);
            Assert.Equal(1, metadata.RootElement.GetProperty("SchemaVersion").GetInt32());
        }
        await fixture.DispatchAsync(); Assert.Single(fixture.Email.Deliveries);
        fixture.Clock.Advance(TimeSpan.FromMinutes(2)); await fixture.PumpAsync();
        Assert.Equal(1, await fixture.NotificationCountAsync(NotificationType.LISTING_WATCH_MATCHED));
    }

    [RbacDatabaseFact]
    public async Task NewOrReactivatedWatchesBackfillExistingListingsAndExpiredOrSuspendedWatchesDoNotPublish()
    {
        await using var fixture = await MatchingFixture.CreateAsync(); var host = fixture.Users[0]; var player = fixture.Users[1];
        var listing = await fixture.SendAsync(new CreateListingCommand(host, fixture.Input()));
        var watch = await fixture.SendAsync(new CreateListingWatchCommand(player, fixture.WatchInput(), false));
        await Assert.ThrowsAsync<ForbiddenException>(() => fixture.SendAsync(new GetListingWatchQuery(host, watch.Id)));
        await Assert.ThrowsAsync<ForbiddenException>(() => fixture.SendAsync(new PauseListingWatchCommand(host, watch.Id, watch.ConcurrencyToken)));
        await Assert.ThrowsAsync<ConflictException>(() => fixture.SendAsync(new PauseListingWatchCommand(player, watch.Id, Guid.NewGuid())));
        watch = await fixture.SendAsync(new PauseListingWatchCommand(player, watch.Id, watch.ConcurrencyToken));
        await fixture.PumpAsync(); Assert.Equal(0, await fixture.NotificationCountAsync(NotificationType.LISTING_WATCH_MATCHED));
        watch = await fixture.SendAsync(new ActivateListingWatchCommand(player, watch.Id, watch.ConcurrencyToken));
        await fixture.PumpAsync(); Assert.Equal(1, await fixture.NotificationCountAsync(NotificationType.LISTING_WATCH_MATCHED));
        var next = await fixture.SendAsync(new CreateListingCommand(host, fixture.Input() with { Title = "Next" }));
        watch = await fixture.SendAsync(new CancelListingWatchCommand(player, watch.Id, watch.ConcurrencyToken));
        await fixture.PumpAsync(); Assert.Equal(1, await fixture.NotificationCountAsync(NotificationType.LISTING_WATCH_MATCHED));
        var suspended = await fixture.SendAsync(new CreateListingWatchCommand(fixture.Users[2], fixture.WatchInput(), false));
        await using (var scope = fixture.Provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
            var user = await db.Users.SingleAsync(item => item.Id == fixture.Users[2]); user.Status = UserStatus.SUSPENDED;
            await db.SaveChangesAsync();
        }
        await fixture.PumpAsync(); Assert.Equal(1, await fixture.NotificationCountAsync(NotificationType.LISTING_WATCH_MATCHED));
        var expiring = await fixture.SendAsync(new CreateListingWatchCommand(player, fixture.WatchInput() with { StartTo = fixture.Clock.GetUtcNow().AddMinutes(1) }, false));
        fixture.Clock.Advance(TimeSpan.FromMinutes(1)); await fixture.PumpAsync();
        Assert.Equal(ListingWatchStatus.EXPIRED, (await fixture.SendAsync(new GetListingWatchQuery(player, expiring.Id))).Status);
        Assert.Equal(1, await fixture.NotificationCountAsync(NotificationType.LISTING_WATCH_MATCHED));
        Assert.NotEqual(listing.Id, next.Id); Assert.Equal(ListingWatchStatus.ACTIVE, suspended.Status);
    }

    [RbacDatabaseFact]
    public async Task LastPlaceAndCancellationRacesCannotBothCommit()
    {
        await using var fixture = await MatchingFixture.CreateAsync(); var host = fixture.Users[0];
        var listing = await fixture.SendAsync(new CreateListingCommand(host, fixture.Input()));
        var a = await fixture.SendAsync(new CreateListingRequestCommand(fixture.Users[1], listing.Id, null, null));
        var b = await fixture.SendAsync(new CreateListingRequestCommand(fixture.Users[2], listing.Id, null, null));
        await using (var first = fixture.Provider.CreateAsyncScope())
        await using (var second = fixture.Provider.CreateAsyncScope())
        {
            // Both tracked contexts start with the same aggregate version and free last place.
            _ = await first.ServiceProvider.GetRequiredService<IMatchingRepository>().GetListingAsync(listing.Id, true, CancellationToken.None);
            _ = await second.ServiceProvider.GetRequiredService<IMatchingRepository>().GetListingAsync(listing.Id, true, CancellationToken.None);
            var results = await Task.WhenAll(TrySendAsync(first.ServiceProvider, new AcceptListingRequestCommand(host, listing.Id, a.Id, a.ConcurrencyToken)),
                TrySendAsync(second.ServiceProvider, new AcceptListingRequestCommand(host, listing.Id, b.Id, b.ConcurrencyToken)));
            Assert.Single(results, result => result);
        }
        listing = await fixture.SendAsync(new GetListingQuery(host, listing.Id)); Assert.Equal((short)1, listing.AcceptedSlots);
        var match = await fixture.SendAsync(new GetMatchQuery(host, listing.MatchId!.Value)); Assert.Equal(2, match.Participants.Count);
        var fresh = await fixture.SendAsync(new CreateListingCommand(host, fixture.Input()));
        var applicant = await fixture.SendAsync(new CreateListingRequestCommand(fixture.Users[1], fresh.Id, null, null));
        fresh = await fixture.SendAsync(new GetListingQuery(host, fresh.Id));
        await using (var first = fixture.Provider.CreateAsyncScope())
        await using (var second = fixture.Provider.CreateAsyncScope())
        {
            _ = await first.ServiceProvider.GetRequiredService<IMatchingRepository>().GetListingAsync(fresh.Id, true, CancellationToken.None);
            _ = await second.ServiceProvider.GetRequiredService<IMatchingRepository>().GetListingAsync(fresh.Id, true, CancellationToken.None);
            var results = await Task.WhenAll(TrySendAsync(first.ServiceProvider, new AcceptListingRequestCommand(host, fresh.Id, applicant.Id, applicant.ConcurrencyToken)),
                TrySendAsync(second.ServiceProvider, new CancelListingCommand(host, fresh.Id, fresh.ConcurrencyToken, "Mưa")));
            Assert.Single(results, result => result);
        }
        fresh = await fixture.SendAsync(new GetListingQuery(host, fresh.Id));
        if (fresh.Status == ListingStatus.CANCELLED) Assert.Null(fresh.MatchId);
        else { Assert.Equal(ListingStatus.MATCHED, fresh.Status); Assert.Equal((short)1, fresh.AcceptedSlots); }
    }

    [RbacDatabaseFact]
    public async Task TeamOpponentFlowUsesCurrentCaptainAndPreventsDeletingTeamsWithCommitments()
    {
        await using var fixture = await MatchingFixture.CreateAsync(); var captain = fixture.Users[0]; var deputy = fixture.Users[1]; var challenger = fixture.Users[2];
        var host = await fixture.SendAsync(new CreateTeamCommand(captain, fixture.SportId, "Host", null, null, null));
        var guest = await fixture.SendAsync(new CreateTeamCommand(challenger, fixture.SportId, "Guest", null, null, null));
        var listing = await fixture.SendAsync(new CreateListingCommand(captain, fixture.Input() with { HostTeamId = host.Id, Mode = ListingMode.FIND_OPPONENT }));
        await Assert.ThrowsAsync<ConflictException>(() => fixture.SendAsync(new DeleteTeamCommand(captain, host.Id, host.ConcurrencyToken)));
        var invitation = await fixture.SendAsync(new InviteTeamMemberCommand(captain, host.Id, deputy));
        _ = await fixture.SendAsync(new AcceptTeamInvitationCommand(deputy, host.Id, invitation.Id, invitation.ConcurrencyToken));
        host = await fixture.SendAsync(new GetTeamQuery(captain, host.Id));
        host = await fixture.SendAsync(new AppointTeamDeputyCommand(captain, host.Id, deputy, host.ConcurrencyToken));
        var request = await fixture.SendAsync(new CreateListingRequestCommand(challenger, listing.Id, guest.Id, "Đá giao hữu"));
        _ = await fixture.SendAsync(new AcceptListingRequestCommand(deputy, listing.Id, request.Id, request.ConcurrencyToken));
        listing = await fixture.SendAsync(new GetListingQuery(captain, listing.Id));
        var match = await fixture.SendAsync(new GetMatchQuery(deputy, listing.MatchId!.Value));
        await Assert.ThrowsAsync<ForbiddenException>(() => fixture.SendAsync(new CancelMatchCommand(deputy, match.Id, match.ConcurrencyToken, "Mưa")));
        host = await fixture.SendAsync(new TransferTeamCaptainCommand(captain, host.Id, deputy, host.ConcurrencyToken));
        await Assert.ThrowsAsync<ForbiddenException>(() => fixture.SendAsync(new CancelMatchCommand(captain, match.Id, match.ConcurrencyToken, "Mưa")));
        _ = await fixture.SendAsync(new CancelMatchCommand(deputy, match.Id, match.ConcurrencyToken, "Mưa"));
        guest = await fixture.SendAsync(new GetTeamQuery(challenger, guest.Id));
        _ = await fixture.SendAsync(new DeleteTeamCommand(challenger, guest.Id, guest.ConcurrencyToken));
        Assert.Equal(deputy, host.CaptainUserId);
    }

    [RbacDatabaseFact]
    public async Task WatchProcessingRetriesWithoutPartialPublicationAndPersistsBatchProgressAcrossScopes()
    {
        await using var fixture = await MatchingFixture.CreateAsync(); var player = fixture.Users[1];
        _ = await fixture.SendAsync(new CreateListingWatchCommand(player, fixture.WatchInput(), true));
        await using (var scope = fixture.Provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
            for (var index = 0; index < 101; index++)
                db.Listings.Add(Listing.Create(fixture.Users[0], new(fixture.SportId, fixture.FormatId, null, ListingMode.FIND_PLAYERS,
                    $"Batch {index}", null, SportLevel.BEGINNER, SportLevel.ADVANCED, 1, fixture.Clock.GetUtcNow().AddHours(4), 90,
                    fixture.Clock.GetUtcNow().AddHours(3), "Sân", 10.8m, 106.7m), fixture.Clock.GetUtcNow()));
            await db.SaveChangesAsync();
        }
        fixture.Email.Configured = false;
        await fixture.ProcessOneAsync(); Assert.Equal(0, await fixture.NotificationCountAsync(NotificationType.LISTING_WATCH_MATCHED));
        await using (var scope = fixture.Provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
            Assert.True(await db.OutboxMessages.AnyAsync(item => item.EventType == MatchingEvents.WatchChanged && item.RetryCount == 1));
            Assert.Empty(await db.ListingWatchDeliveries.ToListAsync());
        }
        fixture.Email.Configured = true; fixture.Clock.Advance(TimeSpan.FromSeconds(6));
        await Task.WhenAll(fixture.ProcessOneAsync(), fixture.ProcessOneAsync());
        await fixture.PumpAsync(); Assert.Equal(101, await fixture.NotificationCountAsync(NotificationType.LISTING_WATCH_MATCHED));
        await using var check = fixture.Provider.CreateAsyncScope();
        Assert.Equal(101, await check.ServiceProvider.GetRequiredService<MatchingSportsDbContext>().ListingWatchDeliveries.CountAsync());
    }

    [RbacDatabaseFact]
    public async Task HttpEndpointsValidateAuthenticateAndUseTheVerifiedActor()
    {
        await using var fixture = await MatchingFixture.CreateAsync();
        var builder = WebApplication.CreateBuilder(); builder.Configuration.AddConfiguration(fixture.Configuration); builder.Logging.ClearProviders();
        builder.Services.AddApplication(null, typeof(ContractMappingProfile).Assembly); builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddSingleton<TimeProvider>(fixture.Clock); builder.Services.AddSingleton<INotificationEmailSender>(fixture.Email);
        builder.Services.AddCurrentUser(); builder.Services.AddProblemDetails(); builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddControllers().AddApplicationPart(typeof(ListingsController).Assembly)
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        await using var app = builder.Build(); app.Urls.Add("http://127.0.0.1:0");
        app.UseExceptionHandler(); app.UseAuthentication(); app.UseAuthorization(); app.MapControllers(); await app.StartAsync();
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/listings")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/v1/users/{fixture.Users[1]}/public-profile")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/v1/teams/{Guid.NewGuid()}/public-profile")).StatusCode);
            var token = await fixture.LoginAsync(0); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var publicUser = await client.GetFromJsonAsync<BaseResponse<PublicUserProfileDto>>(
                $"/api/v1/users/{fixture.Users[1]}/public-profile?ActorUserId={fixture.Users[2]}");
            Assert.Equal(fixture.Users[1], publicUser!.Data.UserId);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/users/{Guid.NewGuid()}/public-profile")).StatusCode);
            var publicTeam = await fixture.SendAsync(new CreateTeamCommand(fixture.Users[1], fixture.SportId, "Public HTTP team", null, null, null));
            var teamResponse = await client.GetFromJsonAsync<BaseResponse<PublicTeamProfileDto>>($"/api/v1/teams/{publicTeam.Id}/public-profile");
            Assert.Equal(publicTeam.Id, teamResponse!.Data.TeamId);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/listings", new { title = "Missing fields" })).StatusCode);
            var input = fixture.Input();
            var created = await client.PostAsJsonAsync("/api/v1/listings", new
            {
                input.SportId,
                input.FormatId,
                Mode = "FIND_PLAYERS",
                input.Title,
                input.OpenSlots,
                input.StartAt,
                input.CutoffAt,
                input.DurationMinutes,
                input.AddressText,
                input.Latitude,
                input.Longitude,
                ActorUserId = fixture.Users[1]
            });
            Assert.Equal(HttpStatusCode.OK, created.StatusCode);
            var json = new JsonSerializerOptions(JsonSerializerDefaults.Web); json.Converters.Add(new JsonStringEnumConverter());
            var listing = (await created.Content.ReadFromJsonAsync<BaseResponse<ListingDto>>(json))!.Data;
            Assert.Equal(fixture.Users[0], listing.OwnerUserId); Assert.Equal(fixture.Users[0], listing.CreatedBy);
            token = await fixture.LoginAsync(1); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/listings?Latitude=10&RadiusMeters=2000")).StatusCode);
            var feed = await client.GetFromJsonAsync<PaginatedResponse<ListingDto>>("/api/v1/listings?Mode=FIND_PLAYERS&Latitude=10.8&Longitude=106.7&RadiusMeters=2000", json);
            Assert.Equal(listing.Id, Assert.Single(feed!.Data).Id);
            var joined = await client.PostAsJsonAsync($"/api/v1/listings/{listing.Id}/requests", new { Message = "Vãng lai", ActorUserId = fixture.Users[2] });
            Assert.Equal(HttpStatusCode.OK, joined.StatusCode);
            var request = (await joined.Content.ReadFromJsonAsync<BaseResponse<ListingRequestDto>>(json))!.Data;
            Assert.Equal(fixture.Users[1], request.ApplicantUserId);
            var watch = await client.PostAsJsonAsync("/api/v1/listing-watches", new
            {
                input.SportId,
                input.FormatId,
                input.Latitude,
                input.Longitude,
                RadiusMeters = 2000,
                StartFrom = fixture.Clock.GetUtcNow(),
                StartTo = fixture.Clock.GetUtcNow().AddDays(1)
            });
            Assert.Equal(HttpStatusCode.OK, watch.StatusCode);
            var watched = (await watch.Content.ReadFromJsonAsync<BaseResponse<ListingWatchDto>>(json))!.Data;
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/v1/listing-watches/{watched.Id}/pause", new { })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/v1/listing-watches/{watched.Id}/pause", new { watched.ConcurrencyToken })).StatusCode);
        }
        finally { await app.StopAsync(); }
    }

    [RbacDatabaseFact]
    public async Task ExpiryReleasesTeamCommitmentsAndWatchQuotaIsProtectedAcrossConcurrentCreates()
    {
        await using var fixture = await MatchingFixture.CreateAsync(); var player = fixture.Users[1]; var host = fixture.Users[0];
        await using (var scope = fixture.Provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>(); var settings = await db.SystemSettings.SingleAsync();
            settings.UpdateValues(settings.GetValues() with { MaxActiveListingWatchesPerUser = 1 }); await db.SaveChangesAsync();
        }
        var results = await Task.WhenAll(TryFixtureSendAsync(fixture, new CreateListingWatchCommand(player, fixture.WatchInput(), false)),
            TryFixtureSendAsync(fixture, new CreateListingWatchCommand(player, fixture.WatchInput(), false)));
        Assert.Single(results, result => result);
        var team = await fixture.SendAsync(new CreateTeamCommand(host, fixture.SportId, "Expiry team", null, null, null));
        var listing = await fixture.SendAsync(new CreateListingCommand(host, fixture.Input() with { HostTeamId = team.Id, OpenSlots = 2 }));
        var request = await fixture.SendAsync(new CreateListingRequestCommand(player, listing.Id, null, null));
        _ = await fixture.SendAsync(new AcceptListingRequestCommand(host, listing.Id, request.Id, request.ConcurrencyToken));
        fixture.Clock.Advance(TimeSpan.FromHours(3));
        await using (var scope = fixture.Provider.CreateAsyncScope())
            Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<MatchingJobs>().ExpireBatchAsync(CancellationToken.None));
        listing = await fixture.SendAsync(new GetListingQuery(host, listing.Id)); Assert.Equal(ListingStatus.EXPIRED, listing.Status);
        var match = await fixture.SendAsync(new GetMatchQuery(host, listing.MatchId!.Value)); Assert.Equal(MatchStatus.CANCELLED, match.Status);
        Assert.Null(match.CancelledByUserId); Assert.Null(match.UpdatedBy);
        _ = await fixture.SendAsync(new DeleteTeamCommand(host, team.Id, team.ConcurrencyToken));
    }
    [RbacDatabaseFact]
    public async Task CompletionAndAdministrativeCancellationCommitHistoryTogether()
    {
        await using var fixture = await MatchingFixture.CreateAsync();
        var host = fixture.Users[0]; var player = fixture.Users[1]; var admin = fixture.Users[2];
        var listing = await fixture.SendAsync(new CreateListingCommand(host, fixture.Input()));
        var request = await fixture.SendAsync(new CreateListingRequestCommand(player, listing.Id, null, null));
        _ = await fixture.SendAsync(new AcceptListingRequestCommand(host, listing.Id, request.Id, request.ConcurrencyToken));
        listing = await fixture.SendAsync(new GetListingQuery(host, listing.Id));
        var match = await fixture.SendAsync(new GetMatchQuery(host, listing.MatchId!.Value));
        match = await fixture.SendAsync(new ConfirmMatchCommand(host, match.Id, match.ConcurrencyToken));
        await Assert.ThrowsAsync<ConflictException>(() => fixture.SendAsync(new CompleteMatchCommand(host, match.Id, match.ConcurrencyToken)));
        fixture.Clock.Advance(TimeSpan.FromMinutes(330));
        match = await fixture.SendAsync(new CompleteMatchCommand(host, match.Id, match.ConcurrencyToken));
        Assert.Equal(MatchStatus.COMPLETED, match.Status);
        Assert.All(match.Participants, item => Assert.Equal(ParticipantStatus.COMPLETED, item.Status));
        Assert.Equal(ListingStatus.COMPLETED, (await fixture.SendAsync(new GetListingQuery(host, listing.Id))).Status);
        Assert.Equal(2, await fixture.NotificationCountAsync(NotificationType.MATCH_COMPLETED));
        await Assert.ThrowsAsync<ForbiddenException>(() => fixture.SendAsync(new CancelAnyMatchCommand(admin, match.Id, match.ConcurrencyToken, "Admin")));
        await using (var scope = fixture.Provider.CreateAsyncScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            Assert.True((await manager.AddToRoleAsync((await manager.FindByIdAsync(admin.ToString()))!, RoleNames.Admin)).Succeeded);
        }
        listing = await fixture.SendAsync(new CreateListingCommand(host, fixture.Input()));
        request = await fixture.SendAsync(new CreateListingRequestCommand(player, listing.Id, null, null));
        _ = await fixture.SendAsync(new AcceptListingRequestCommand(host, listing.Id, request.Id, request.ConcurrencyToken));
        listing = await fixture.SendAsync(new GetListingQuery(host, listing.Id));
        match = await fixture.SendAsync(new GetMatchQuery(host, listing.MatchId!.Value));
        fixture.Clock.Advance(TimeSpan.FromHours(4));
        await Assert.ThrowsAsync<ConflictException>(() => fixture.SendAsync(new CancelMatchCommand(host, match.Id, match.ConcurrencyToken, "Trễ")));
        var cancelled = await fixture.SendAsync(new CancelAnyMatchCommand(admin, match.Id, match.ConcurrencyToken, "  Sân không an toàn  "));
        _ = await fixture.SendAsync(new CancelAnyMatchCommand(admin, match.Id, match.ConcurrencyToken, "Lặp lại"));
        Assert.Equal(admin, cancelled.CancelledByUserId); Assert.Equal(admin, cancelled.UpdatedBy);
        await using var check = fixture.Provider.CreateAsyncScope();
        var db = check.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
        var action = Assert.Single(await db.AdminActions.Where(item => item.TargetId == match.Id).ToListAsync());
        Assert.Equal(AdminActionType.CANCEL_MATCH, action.ActionType); Assert.Equal(admin, action.AdminUserId);
        Assert.Equal("Sân không an toàn", action.Reason);
        using var snapshot = JsonDocument.Parse(action.AfterJson);
        Assert.False(snapshot.RootElement.TryGetProperty("ConcurrencyToken", out _));
        Assert.Equal(2, await db.NotificationHistories.CountAsync(item => item.Type == NotificationType.MATCH_CANCELLED && item.EntityId == match.Id));
    }

    private static async Task<bool> TryFixtureSendAsync<T>(MatchingFixture fixture, IRequest<T> request)
    {
        try { _ = await fixture.SendAsync(request); return true; }
        catch (ConflictException) { return false; }
    }

    private static async Task<bool> TrySendAsync<T>(IServiceProvider services, IRequest<T> request)
    {
        try { _ = await services.GetRequiredService<IMediator>().Send(request); return true; }
        catch (ConflictException) { return false; }
    }

    [RbacDatabaseFact]
    public async Task ReminderUsesCurrentSettingAndSurvivesConcurrentWorkersAndHistoryCleanup()
    {
        await using var fixture = await MatchingFixture.CreateAsync();
        var match = await CreateConfirmedMatchAsync(fixture);
        fixture.Clock.Advance(TimeSpan.FromHours(2));
        Assert.Equal(0, await fixture.RemindAsync());
        await using (var scope = fixture.Provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
            var settings = await db.SystemSettings.SingleAsync();
            settings.UpdateValues(settings.GetValues() with { MatchReminderLeadMinutes = 120 });
            await db.SaveChangesAsync();
        }
        // A worker may lose the Serializable race; a later cycle retries without duplicating publication.
        var results = await Task.WhenAll(Enumerable.Range(0, 3).Select(async _ =>
        {
            try { return await fixture.RemindAsync(); }
            catch (ConflictException) { return 0; }
        }));
        Assert.Equal(1, results.Sum());
        Assert.Equal(2, await fixture.NotificationCountAsync(NotificationType.MATCH_REMINDER));
        await using (var scope = fixture.Provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
            var histories = await db.NotificationHistories.Where(h => h.Type == NotificationType.MATCH_REMINDER).ToListAsync();
            Assert.All(histories, h => { Assert.Null(h.CreatedBy); Assert.Equal(match.Id, h.EntityId); Assert.Equal(match.StartAt, h.ExpiresAt); });
            Assert.NotNull((await db.Matches.SingleAsync(m => m.Id == match.Id)).ReminderPublishedAt);
            db.NotificationHistories.RemoveRange(histories);
            await db.SaveChangesAsync();
        }
        Assert.Equal(0, await fixture.RemindAsync());
        Assert.Equal(0, await fixture.NotificationCountAsync(NotificationType.MATCH_REMINDER));
    }

    [RbacDatabaseFact]
    public async Task ReminderSkipsCancelledPendingAndAlreadyStartedMatches()
    {
        await using var fixture = await MatchingFixture.CreateAsync();
        var match = await CreateConfirmedMatchAsync(fixture);
        await fixture.SendAsync(new CancelMatchCommand(fixture.Users[0], match.Id, match.ConcurrencyToken, "Mưa"));
        fixture.Clock.Advance(TimeSpan.FromHours(3));
        Assert.Equal(0, await fixture.RemindAsync());
        var listing = await fixture.SendAsync(new CreateListingCommand(fixture.Users[0], fixture.Input()));
        var request = await fixture.SendAsync(new CreateListingRequestCommand(fixture.Users[1], listing.Id, null, null));
        await fixture.SendAsync(new AcceptListingRequestCommand(fixture.Users[0], listing.Id, request.Id, request.ConcurrencyToken));
        fixture.Clock.Advance(TimeSpan.FromHours(3));
        Assert.Equal(0, await fixture.RemindAsync()); // PENDING, even inside the configured window.
        listing = await fixture.SendAsync(new GetListingQuery(fixture.Users[0], listing.Id));
        var pending = await fixture.SendAsync(new GetMatchQuery(fixture.Users[0], listing.MatchId!.Value));
        await fixture.SendAsync(new ConfirmMatchCommand(fixture.Users[0], pending.Id, pending.ConcurrencyToken));
        fixture.Clock.Advance(TimeSpan.FromHours(1));
        Assert.Equal(0, await fixture.RemindAsync());
        Assert.Equal(0, await fixture.NotificationCountAsync(NotificationType.MATCH_REMINDER));
    }

    [RbacDatabaseFact]
    public async Task ReminderEmailIsSkippedAfterCancellationOrKickoff()
    {
        await using var fixture = await MatchingFixture.CreateAsync();
        var match = await CreateConfirmedMatchAsync(fixture);
        fixture.Clock.Advance(TimeSpan.FromHours(3));
        await using (var scope = fixture.Provider.CreateAsyncScope())
        {
            var notifications = new MatchingNotifications(scope.ServiceProvider.GetRequiredService<IMatchingRepository>(),
                scope.ServiceProvider.GetRequiredService<INotificationWorkRepository>(), fixture.Email, fixture.Clock, true);
            var jobs = new MatchingJobs(scope.ServiceProvider.GetRequiredService<IMatchingRepository>(),
                scope.ServiceProvider.GetRequiredService<IMatchingWorkRepository>(), scope.ServiceProvider.GetRequiredService<MatchingServices>(),
                notifications, scope.ServiceProvider.GetRequiredService<IUnitOfWork>(), scope.ServiceProvider.GetRequiredService<IAuditActorContext>(), fixture.Clock);
            Assert.Equal(1, await jobs.PublishRemindersBatchAsync(CancellationToken.None));
        }
        match = await fixture.SendAsync(new GetMatchQuery(fixture.Users[0], match.Id));
        await fixture.SendAsync(new CancelMatchCommand(fixture.Users[0], match.Id, match.ConcurrencyToken, "Mưa"));
        await fixture.DispatchAsync();
        Assert.Empty(fixture.Email.Deliveries);
        await using var readScope = fixture.Provider.CreateAsyncScope();
        var histories = await readScope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>().NotificationHistories
            .Where(h => h.Type == NotificationType.MATCH_REMINDER).ToListAsync();
        Assert.Equal(2, histories.Count);
        Assert.All(histories, h => Assert.Equal(NotificationDeliveryStatus.SKIPPED, h.EmailStatus));
    }

    [RbacDatabaseFact]
    public async Task PublicProfilesExposeSafeFieldsAndHideUnavailableTargets()
    {
        await using var fixture = await MatchingFixture.CreateAsync();
        var actor = fixture.Users[0]; var target = fixture.Users[1];
        var team = await fixture.SendAsync(new CreateTeamCommand(target, fixture.SportId, "Public team", "Giới thiệu", "Quận 1", null));
        await using (var scope = fixture.Provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
            var profile = await db.UserProfiles.SingleAsync(p => p.UserId == target);
            profile.UpdateDetails("Public player", "avatar.png", "Quận 1", "PRIVATE CONTACT");
            var sport = await db.Sports.Include(s => s.Formats).SingleAsync(s => s.Id == fixture.SportId);
            db.UserSportProfiles.Add(UserSportProfile.Create(target, sport, "INTERMEDIATE", [fixture.FormatId], "Thích bóng đá"));
            await db.SaveChangesAsync();
        }
        var userView = await fixture.SendAsync(new GetPublicUserProfileQuery(actor, target));
        Assert.Equal(target, userView.UserId);
        Assert.Equal("Public player", userView.DisplayName);
        Assert.Equal(SportLevel.INTERMEDIATE, Assert.Single(userView.SportProfiles).Level);
        Assert.Equal(fixture.FormatId, Assert.Single(userView.SportProfiles[0].PreferredFormats).FormatId);
        var teamView = await fixture.SendAsync(new GetPublicTeamProfileQuery(actor, team.Id));
        Assert.Equal(1, teamView.ActiveMemberCount);
        Assert.Equal(target, teamView.Captain.UserId);
        var json = JsonSerializer.Serialize(new { userView, teamView });
        Assert.DoesNotContain("PRIVATE CONTACT", json);
        Assert.DoesNotContain("@example.test", json);
        Assert.DoesNotContain("ConcurrencyToken", json);
        Assert.DoesNotContain("CreatedBy", json);
        Assert.DoesNotContain("Invitation", json);
        await using (var scope = fixture.Provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
            (await db.Users.SingleAsync(u => u.Id == target)).Status = UserStatus.SUSPENDED;
            await db.SaveChangesAsync();
        }
        Assert.Equal(ErrorCodes.UserProfileNotFound, (await Assert.ThrowsAsync<NotFoundException>(() =>
            fixture.SendAsync(new GetPublicUserProfileQuery(actor, target)))).Code);
        await Assert.ThrowsAsync<NotFoundException>(() => fixture.SendAsync(new GetPublicTeamProfileQuery(actor, team.Id)));
        await Assert.ThrowsAsync<NotFoundException>(() => fixture.SendAsync(new GetPublicUserProfileQuery(actor, Guid.NewGuid())));
    }

    [RbacDatabaseFact]
    public async Task TeamReminderNotifiesCurrentActiveMembersAndOnlyOncePerUser()
    {
        await using var fixture = await MatchingFixture.CreateAsync();
        var host = await fixture.SendAsync(new CreateTeamCommand(fixture.Users[0], fixture.SportId, "Host", null, null, null));
        var guest = await fixture.SendAsync(new CreateTeamCommand(fixture.Users[2], fixture.SportId, "Guest", null, null, null));
        var invite = await fixture.SendAsync(new InviteTeamMemberCommand(fixture.Users[0], host.Id, fixture.Users[1]));
        await fixture.SendAsync(new AcceptTeamInvitationCommand(fixture.Users[1], host.Id, invite.Id, invite.ConcurrencyToken));
        var listing = await fixture.SendAsync(new CreateListingCommand(fixture.Users[0],
            fixture.Input() with { HostTeamId = host.Id, Mode = ListingMode.FIND_OPPONENT }));
        var request = await fixture.SendAsync(new CreateListingRequestCommand(fixture.Users[2], listing.Id, guest.Id, null));
        await fixture.SendAsync(new AcceptListingRequestCommand(fixture.Users[0], listing.Id, request.Id, request.ConcurrencyToken));
        listing = await fixture.SendAsync(new GetListingQuery(fixture.Users[0], listing.Id));
        var match = await fixture.SendAsync(new GetMatchQuery(fixture.Users[0], listing.MatchId!.Value));
        await fixture.SendAsync(new ConfirmMatchCommand(fixture.Users[0], match.Id, match.ConcurrencyToken));
        fixture.Clock.Advance(TimeSpan.FromHours(3));
        Assert.Equal(1, await fixture.RemindAsync());
        await using var scope = fixture.Provider.CreateAsyncScope();
        var recipients = await scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>().NotificationHistories
            .Where(h => h.Type == NotificationType.MATCH_REMINDER).Select(h => h.UserId).ToListAsync();
        Assert.Equal(fixture.Users.Order(), recipients.Select(id => id!.Value).Order());
        Assert.Equal(0, await fixture.RemindAsync());
    }

    [RbacDatabaseFact]
    public async Task ReminderFailureRollsBackPublicationAndExpiredEmailCannotSend()
    {
        await using var fixture = await MatchingFixture.CreateAsync();
        var match = await CreateConfirmedMatchAsync(fixture);
        fixture.Clock.Advance(TimeSpan.FromHours(3));
        fixture.Email.Configured = false;
        await Assert.ThrowsAsync<ServiceUnavailableException>(() => fixture.RemindWithEmailAsync());
        Assert.Equal(0, await fixture.NotificationCountAsync(NotificationType.MATCH_REMINDER));
        await using (var scope = fixture.Provider.CreateAsyncScope())
            Assert.Null((await scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>().Matches.SingleAsync(m => m.Id == match.Id)).ReminderPublishedAt);
        fixture.Email.Configured = true;
        Assert.Equal(1, await fixture.RemindWithEmailAsync());
        fixture.Clock.Advance(TimeSpan.FromHours(1));
        await fixture.DispatchAsync();
        Assert.Empty(fixture.Email.Deliveries);
        await using var read = fixture.Provider.CreateAsyncScope();
        var histories = await read.ServiceProvider.GetRequiredService<MatchingSportsDbContext>().NotificationHistories
            .Where(h => h.Type == NotificationType.MATCH_REMINDER).ToListAsync();
        Assert.Equal(2, histories.Count);
        Assert.All(histories, h => Assert.Equal(NotificationDeliveryStatus.SKIPPED, h.EmailStatus));
    }

    private static async Task<MatchDto> CreateConfirmedMatchAsync(MatchingFixture fixture)
    {
        var listing = await fixture.SendAsync(new CreateListingCommand(fixture.Users[0], fixture.Input()));
        var request = await fixture.SendAsync(new CreateListingRequestCommand(fixture.Users[1], listing.Id, null, null));
        await fixture.SendAsync(new AcceptListingRequestCommand(fixture.Users[0], listing.Id, request.Id, request.ConcurrencyToken));
        listing = await fixture.SendAsync(new GetListingQuery(fixture.Users[0], listing.Id));
        var match = await fixture.SendAsync(new GetMatchQuery(fixture.Users[0], listing.MatchId!.Value));
        return await fixture.SendAsync(new ConfirmMatchCommand(fixture.Users[0], match.Id, match.ConcurrencyToken));
    }

    private sealed class MatchingFixture : IAsyncDisposable
    {
        public ServiceProvider Provider { get; private set; } = null!;
        public IConfiguration Configuration { get; private set; } = null!;
        public MatchingClock Clock { get; } = new();
        public TestEmailSender Email { get; } = new();
        public Guid[] Users { get; private set; } = [];
        public Guid SportId { get; private set; }
        public Guid FormatId { get; private set; }
        public Guid OtherSportId { get; private set; }
        public Guid OtherFormatId { get; private set; }
        public static async Task<MatchingFixture> CreateAsync()
        {
            var fixture = new MatchingFixture();
            var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("MATCHINGSPORTS_RBAC_TEST_CONNECTION")!);
            Assert.StartsWith("matchingsports_rbac_test_", connection.Database, StringComparison.Ordinal);
            connection.Database = $"matchingsports_rbac_test_matching_{Guid.NewGuid():N}";
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["ConnectionStrings:matchingsports"] = connection.ConnectionString, ["Auth:Jwt:SigningKey"] = new string('s', 48), ["Auth:Otp:Pepper"] = new string('p', 48) }).Build();
            var services = new ServiceCollection(); services.AddLogging(); services.AddSingleton<IConfiguration>(configuration);
            services.AddApplication(null, typeof(ContractMappingProfile).Assembly); services.AddInfrastructure(configuration);
            services.AddSingleton<TimeProvider>(fixture.Clock); services.AddSingleton<INotificationEmailSender>(fixture.Email);
            fixture.Configuration = configuration;
            fixture.Provider = services.BuildServiceProvider();
            try
            {
                await fixture.Provider.InitializeDatabaseAsync(true);
                await using var scope = fixture.Provider.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
                var manager = scope.ServiceProvider.GetRequiredService<UserManager<User>>(); var ids = new List<Guid>();
                for (var index = 0; index < 3; index++)
                {
                    var user = User.CreateVerifiedAccount($"matching-{index}@example.test", $"Player {index}");
                    Assert.True((await manager.CreateAsync(user, "Testing-Password123!")).Succeeded);
                    Assert.True((await manager.AddToRoleAsync(user, RoleNames.User)).Succeeded); ids.Add(user.Id);
                }
                fixture.Users = ids.ToArray();
                var football = await db.Sports.Include(item => item.Formats).SingleAsync(item => item.Code == "FOOTBALL");
                fixture.SportId = football.Id; fixture.FormatId = football.Formats.First().Id;
                var badminton = await db.Sports.Include(item => item.Formats).SingleAsync(item => item.Code == "BADMINTON");
                fixture.OtherSportId = badminton.Id; fixture.OtherFormatId = badminton.Formats.First().Id;
                return fixture;
            }
            catch { await fixture.DisposeAsync(); throw; }
        }
        public ListingInput Input() => new(SportId, FormatId, null, ListingMode.FIND_PLAYERS, "Bóng đá vãng lai", null,
            SportLevel.BEGINNER, SportLevel.ADVANCED, 1, Clock.GetUtcNow().AddHours(4), 90, Clock.GetUtcNow().AddHours(3), "Sân bóng", 10.8m, 106.7m);
        public ListingWatchInput WatchInput() => new(SportId, FormatId, 10.8m, 106.7m, 2000, Clock.GetUtcNow(), Clock.GetUtcNow().AddDays(1));
        public async Task<string> LoginAsync(int userIndex)
        {
            await using var scope = Provider.CreateAsyncScope();
            return (await scope.ServiceProvider.GetRequiredService<IIdentityService>().LoginAsync($"matching-{userIndex}@example.test", "Testing-Password123!", null, null, CancellationToken.None)).AccessToken;
        }
        public async Task<T> SendAsync<T>(IRequest<T> request)
        { await using var scope = Provider.CreateAsyncScope(); return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request); }
        public async Task<int> RemindAsync()
        { await using var scope = Provider.CreateAsyncScope(); return await scope.ServiceProvider.GetRequiredService<MatchingJobs>().PublishRemindersBatchAsync(CancellationToken.None); }
        public async Task<int> RemindWithEmailAsync()
        {
            await using var scope = Provider.CreateAsyncScope();
            var notifications = new MatchingNotifications(scope.ServiceProvider.GetRequiredService<IMatchingRepository>(),
                scope.ServiceProvider.GetRequiredService<INotificationWorkRepository>(), Email, Clock, true);
            var jobs = new MatchingJobs(scope.ServiceProvider.GetRequiredService<IMatchingRepository>(),
                scope.ServiceProvider.GetRequiredService<IMatchingWorkRepository>(), scope.ServiceProvider.GetRequiredService<MatchingServices>(),
                notifications, scope.ServiceProvider.GetRequiredService<IUnitOfWork>(), scope.ServiceProvider.GetRequiredService<IAuditActorContext>(), Clock);
            return await jobs.PublishRemindersBatchAsync(CancellationToken.None);
        }
        public async Task<bool> ProcessOneAsync()
        { await using var scope = Provider.CreateAsyncScope(); return await scope.ServiceProvider.GetRequiredService<MatchingJobs>().ProcessNextAsync(CancellationToken.None); }
        public async Task PumpAsync()
        {
            for (var index = 0; index < 1000; index++) if (!await ProcessOneAsync()) return;
            Assert.Fail("Matching worker did not exhaust due batches.");
        }
        public async Task DispatchAsync()
        {
            for (var index = 0; index < 1000; index++)
            { await using var scope = Provider.CreateAsyncScope(); if (!await scope.ServiceProvider.GetRequiredService<NotificationJobs>().DispatchNextEmailAsync(CancellationToken.None)) return; }
            Assert.Fail("Email worker did not exhaust due messages.");
        }
        public async Task<int> NotificationCountAsync(NotificationType? type = null)
        { await using var scope = Provider.CreateAsyncScope(); return await scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>().NotificationHistories.CountAsync(item => type == null || item.Type == type); }
        public async ValueTask DisposeAsync()
        {
            if (Provider is null) return;
            await using (var scope = Provider.CreateAsyncScope()) await scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>().Database.EnsureDeletedAsync();
            await Provider.DisposeAsync();
        }
    }
    private sealed class MatchingClock : TimeProvider
    {
        private DateTimeOffset now = new(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan duration) => now += duration;
    }
    private sealed class TestEmailSender : INotificationEmailSender
    {
        public bool Configured { get; set; } = true;
        public bool IsConfigured => Configured;
        public ConcurrentBag<string> Deliveries { get; } = [];
        public Task SendAsync(string email, string title, string html, string idempotencyKey, CancellationToken cancellationToken)
        { Deliveries.Add(idempotencyKey); return Task.CompletedTask; }
    }
}
