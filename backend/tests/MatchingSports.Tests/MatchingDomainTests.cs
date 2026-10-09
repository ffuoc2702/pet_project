using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Domain.ValueObjects;

namespace MatchingSports.Tests;

public sealed class MatchingDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 10, 0, 0, TimeSpan.Zero);
    private static ListingValues Values() => new(Guid.NewGuid(), Guid.NewGuid(), null, ListingMode.FIND_PLAYERS,
        "  Bóng đá vãng lai  ", "  ghi chú  ", SportLevel.BEGINNER, SportLevel.ADVANCED, 1,
        Now.AddHours(5), 90, Now.AddHours(4), "  Sân bóng  ", 10.8m, 106.7m);

    [Fact]
    public void ListingNormalizesValuesAndKeepsTimeInUtc()
    {
        var listing = Listing.Create(Guid.NewGuid(), Values() with { StartAt = Now.AddHours(5).ToOffset(TimeSpan.FromHours(7)) }, Now);
        Assert.Equal("Bóng đá vãng lai", listing.Title);
        Assert.Equal("ghi chú", listing.Note);
        Assert.Equal("Sân bóng", listing.AddressText);
        Assert.Equal(TimeSpan.Zero, listing.StartAt.Offset);
        Assert.True(listing.IsAvailable(Now));
    }
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void InvalidListingValuesCannotPartiallyMutateAnExistingListing(int invalidCase)
    {
        var values = Values();
        var listing = Listing.Create(Guid.NewGuid(), values, Now);
        var invalid = invalidCase switch
        {
            0 => values with { OpenSlots = 0 },
            1 => values with { CutoffAt = Now },
            2 => values with { LevelMin = SportLevel.ADVANCED, LevelMax = SportLevel.BEGINNER },
            3 => values with { Latitude = 91 },
            4 => values with { Title = " " },
            _ => values with { DurationMinutes = 14 }
        };
        Assert.Throws<MatchingRuleException>(() => listing.UpdateDetails(invalid, Now));
        Assert.Equal("Bóng đá vãng lai", listing.Title);
        Assert.Equal(values.StartAt, listing.StartAt);
        Assert.Equal((short)1, listing.OpenSlots);
    }
    [Fact]
    public void PendingRequestsDoNotHoldSlotsAndAcceptanceCannotOverfill()
    {
        var listing = Listing.Create(Guid.NewGuid(), Values(), Now);
        var first = listing.RequestToJoin(Guid.NewGuid(), null, null, Now);
        var second = listing.RequestToJoin(Guid.NewGuid(), null, null, Now);
        Assert.Equal((short)0, listing.AcceptedSlots);
        listing.AcceptRequest(first, listing.OwnerUserId, Now);
        Assert.Equal(ListingStatus.MATCHED, listing.Status);
        Assert.Equal(MatchStatus.PENDING, listing.Match!.Status);
        Assert.Throws<MatchingRuleException>(() => listing.AcceptRequest(second, listing.OwnerUserId, Now));
        Assert.Equal(RequestStatus.PENDING, second.Status);
        Assert.Equal((short)1, listing.AcceptedSlots);
        Assert.Equal(2, listing.Match.Participants.Count);
    }
    [Fact]
    public void ApplicantCannotSubmitDuplicateOrJoinOwnListing()
    {
        var owner = Guid.NewGuid(); var user = Guid.NewGuid();
        var listing = Listing.Create(owner, Values(), Now);
        Assert.Throws<MatchingRuleException>(() => listing.RequestToJoin(owner, null, null, Now));
        listing.RequestToJoin(user, null, null, Now);
        Assert.Throws<MatchingRuleException>(() => listing.RequestToJoin(user, null, null, Now));
        Assert.Single(listing.Requests);
    }
    [Fact]
    public void AcceptedPlayerCanWithdrawAndRejoinUsingTheExistingParticipant()
    {
        var listing = Listing.Create(Guid.NewGuid(), Values(), Now);
        var user = Guid.NewGuid();
        var first = listing.RequestToJoin(user, null, null, Now);
        listing.AcceptRequest(first, listing.OwnerUserId, Now);
        var participantId = listing.Match!.Participants.Single(item => item.UserId == user).Id;
        listing.WithdrawRequest(first, Now);
        Assert.Equal(ListingStatus.OPEN, listing.Status);
        Assert.Equal((short)0, listing.AcceptedSlots);
        var next = listing.RequestToJoin(user, null, null, Now);
        listing.AcceptRequest(next, listing.OwnerUserId, Now);
        var participant = listing.Match.Participants.Single(item => item.UserId == user);
        Assert.Equal(participantId, participant.Id);
        Assert.Equal(next.Id, participant.SourceRequestId);
        Assert.Equal(ParticipantStatus.CONFIRMED, participant.Status);
    }
    [Fact]
    public void ConfirmationClosesPendingRequestsAndPreventsWithdrawalOrScheduleChange()
    {
        var values = Values(); var listing = Listing.Create(Guid.NewGuid(), values, Now);
        var accepted = listing.RequestToJoin(Guid.NewGuid(), null, null, Now);
        var pending = listing.RequestToJoin(Guid.NewGuid(), null, null, Now);
        Assert.Throws<MatchingRuleException>(() => listing.ConfirmMatch(Now));
        listing.AcceptRequest(accepted, listing.OwnerUserId, Now);
        listing.ConfirmMatch(Now);
        Assert.Equal(MatchStatus.CONFIRMED, listing.Match!.Status);
        Assert.Equal(RequestStatus.EXPIRED, pending.Status);
        Assert.Throws<MatchingRuleException>(() => listing.WithdrawRequest(accepted, Now));
        Assert.Throws<MatchingRuleException>(() => listing.UpdateDetails(values with { Title = "Changed" }, Now));
    }
    [Fact]
    public void CancellationKeepsHistoryAndIsIdempotent()
    {
        var listing = Listing.Create(Guid.NewGuid(), Values(), Now);
        var application = listing.RequestToJoin(Guid.NewGuid(), null, null, Now);
        listing.AcceptRequest(application, listing.OwnerUserId, Now);
        Assert.True(listing.Cancel(listing.OwnerUserId, "  Mưa lớn  ", Now));
        Assert.False(listing.Cancel(listing.OwnerUserId, "Mưa", Now));
        Assert.Equal("Mưa lớn", listing.CancelReason);
        Assert.Equal(RequestStatus.CANCELLED, application.Status);
        Assert.All(listing.Match!.Participants, participant => Assert.Equal(ParticipantStatus.CANCELLED, participant.Status));
    }
    [Fact]
    public void ExpiryClosesUnfilledMatchAndFullUnconfirmedMatchAtTheirCorrectBoundaries()
    {
        var listing = Listing.Create(Guid.NewGuid(), Values() with { OpenSlots = 2 }, Now);
        var application = listing.RequestToJoin(Guid.NewGuid(), null, null, Now);
        listing.AcceptRequest(application, listing.OwnerUserId, Now);
        Assert.False(listing.Expire(listing.CutoffAt.AddTicks(-1)));
        Assert.True(listing.Expire(listing.CutoffAt));
        Assert.Equal(RequestStatus.EXPIRED, application.Status);
        Assert.Equal(MatchStatus.CANCELLED, listing.Match!.Status);
        var full = Listing.Create(Guid.NewGuid(), Values(), Now);
        full.AcceptRequest(full.RequestToJoin(Guid.NewGuid(), null, null, Now), full.OwnerUserId, Now);
        Assert.False(full.Expire(full.CutoffAt));
        Assert.True(full.Expire(full.StartAt));
    }
    [Fact]
    public void OpponentModeRequiresAnotherTeamAndOneSlot()
    {
        var values = Values() with { Mode = ListingMode.FIND_OPPONENT, HostTeamId = Guid.NewGuid() };
        Assert.Throws<MatchingRuleException>(() => Listing.Create(Guid.NewGuid(), values with { OpenSlots = 2 }, Now));
        var listing = Listing.Create(Guid.NewGuid(), values, Now);
        Assert.Throws<MatchingRuleException>(() => listing.RequestToJoin(Guid.NewGuid(), null, null, Now));
        Assert.Throws<MatchingRuleException>(() => listing.RequestToJoin(null, listing.HostTeamId, null, Now));
        var request = listing.RequestToJoin(null, Guid.NewGuid(), null, Now);
        listing.AcceptRequest(request, listing.OwnerUserId, Now);
        Assert.Equal((short)2, listing.Match!.Participants.Single(item => item.Role == ParticipantRole.OPPONENT).SideNo);
    }
    [Fact]
    public void WatchHasExplicitLifecycleAndCannotBeActivatedAfterExpiry()
    {
        var criteria = new ListingCriteria(Guid.NewGuid(), StartFrom: Now, StartTo: Now.AddDays(1), Latitude: 10, Longitude: 106, RadiusMeters: 1000);
        var settings = SystemSettings.CreateDefaults();
        var watch = ListingWatch.Create(Guid.NewGuid(), criteria, false, settings, Now);
        watch.Pause(Now); Assert.False(watch.IsActive(Now));
        watch.Activate(settings, Now); Assert.True(watch.IsActive(Now));
        Assert.Equal(ListingWatchStatus.EXPIRED, watch.EffectiveStatus(criteria.StartTo!.Value));
        Assert.Throws<MatchingRuleException>(() => watch.Activate(settings, criteria.StartTo.Value));
        watch.Expire(criteria.StartTo.Value);
        watch.UpdateCriteria(criteria with { StartFrom = Now.AddDays(2), StartTo = Now.AddDays(3) }, false, settings, Now.AddDays(1));
        Assert.Equal(ListingWatchStatus.PAUSED, watch.Status);
        watch.Cancel();
        Assert.Throws<MatchingRuleException>(() => watch.UpdateCriteria(criteria, false, settings, Now));
    }
    [Theory]
    [InlineData(0)]
    [InlineData(50001)]
    public void WatchRejectsRadiusOutsideTheConfiguredLimit(int radius)
    {
        var criteria = new ListingCriteria(Guid.NewGuid(), StartFrom: Now, StartTo: Now.AddDays(1), Latitude: 10, Longitude: 106, RadiusMeters: radius);
        Assert.Throws<MatchingRuleException>(() => ListingWatch.Create(Guid.NewGuid(), criteria, false, SystemSettings.CreateDefaults(), Now));
    }
    [Fact]
    public void CompleteRequiresTheEndOfTheConfirmedMatchAndCannotThenBeCancelled()
    {
        var listing = Listing.Create(Guid.NewGuid(), Values(), Now);
        listing.AcceptRequest(listing.RequestToJoin(Guid.NewGuid(), null, null, Now), listing.OwnerUserId, Now);
        listing.ConfirmMatch(Now);
        Assert.Throws<MatchingRuleException>(() => listing.CompleteMatch(listing.StartAt));
        listing.CompleteMatch(listing.StartAt.AddMinutes(listing.DurationMinutes));
        Assert.Equal(ListingStatus.COMPLETED, listing.Status);
        Assert.Throws<MatchingRuleException>(() => listing.Cancel(listing.OwnerUserId, "Cancel", Now, true));
    }
}
