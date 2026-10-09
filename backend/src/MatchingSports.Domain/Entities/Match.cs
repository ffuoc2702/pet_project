using MatchingSports.Domain.Enums;

namespace MatchingSports.Domain.Entities;

public sealed class Match : ConcurrentAuditableEntity
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public MatchStatus Status { get; set; } = MatchStatus.PENDING;
    public DateTimeOffset StartAt { get; set; }
    public short DurationMinutes { get; set; }
    public string AddressText { get; set; } = null!;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? ReminderPublishedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public string? CancelReason { get; set; }

    public Listing Listing { get; set; } = null!;
    public User? CancelledByUser { get; set; }
    public ICollection<MatchParticipant> Participants { get; set; } = [];

    public bool IsReminderDue(DateTimeOffset now, TimeSpan leadTime)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(leadTime, TimeSpan.Zero);
        return Status == MatchStatus.CONFIRMED && ReminderPublishedAt is null
            && now < StartAt && now >= StartAt - leadTime;
    }

    public void RecordReminderPublished(DateTimeOffset now, TimeSpan leadTime)
    {
        MatchingRules.Require(IsReminderDue(now, leadTime), "Trận chưa đến hạn nhắc hoặc đã được nhắc.");
        ReminderPublishedAt = now;
        RenewConcurrencyToken();
    }

    internal static Match Create(Listing listing)
    {
        var match = new Match { Id = Guid.NewGuid(), ListingId = listing.Id };
        match.RefreshSchedule(listing);
        match.Participants.Add(MatchParticipant.Create(match.Id, listing.HostTeamId is null ? listing.OwnerUserId : null,
            listing.HostTeamId, null, 1, ParticipantRole.HOST));
        return match;
    }

    internal void RefreshSchedule(Listing listing)
    {
        MatchingRules.Require(Status == MatchStatus.PENDING, "Không được sửa lịch trận đã xác nhận.");
        StartAt = listing.StartAt; DurationMinutes = listing.DurationMinutes; AddressText = listing.AddressText;
        Latitude = listing.Latitude; Longitude = listing.Longitude;
    }
    internal void AddApplicant(ListingRequest request)
    {
        MatchingRules.Require(Status == MatchStatus.PENDING, "Trận không còn nhận người tham gia.");
        var existing = Participants.SingleOrDefault(participant => request.ApplicantUserId is not null
            ? participant.UserId == request.ApplicantUserId : participant.TeamId == request.ApplicantTeamId);
        MatchingRules.Require(existing is null || existing.Status == ParticipantStatus.CANCELLED, "Đã tham gia trận.");
        if (existing is null)
            Participants.Add(MatchParticipant.Create(Id, request.ApplicantUserId, request.ApplicantTeamId, request.Id,
                request.Type == RequestType.OPPONENT ? (short)2 : (short)1,
                request.Type == RequestType.OPPONENT ? ParticipantRole.OPPONENT : ParticipantRole.PLAYER));
        else existing.Rejoin(request.Id);
        RenewConcurrencyToken();
    }
    internal void WithdrawApplicant(ListingRequest request)
    {
        MatchingRules.Require(Status == MatchStatus.PENDING, "Trận đã xác nhận không được rút chỗ.");
        var participant = Participants.Single(item => item.SourceRequestId == request.Id && item.Status == ParticipantStatus.CONFIRMED);
        participant.Cancel(); RenewConcurrencyToken();
    }
    internal void Confirm(DateTimeOffset now)
    {
        MatchingRules.Require(Status == MatchStatus.PENDING && now < StartAt, "Trận không được xác nhận.");
        Status = MatchStatus.CONFIRMED; ConfirmedAt = now; RenewConcurrencyToken();
    }
    internal void Cancel(Guid? actorId, string reason, DateTimeOffset now)
    {
        MatchingRules.Require(Status is MatchStatus.PENDING or MatchStatus.CONFIRMED, "Trận đã kết thúc.");
        Status = MatchStatus.CANCELLED; CancelledAt = now; CancelledByUserId = actorId; CancelReason = reason;
        foreach (var participant in Participants.Where(item => item.Status == ParticipantStatus.CONFIRMED)) participant.Cancel();
        RenewConcurrencyToken();
    }
    internal void Complete(DateTimeOffset now)
    {
        MatchingRules.Require(Status == MatchStatus.CONFIRMED && now >= StartAt.AddMinutes(DurationMinutes), "Trận chưa thể hoàn tất.");
        Status = MatchStatus.COMPLETED; CompletedAt = now;
        foreach (var participant in Participants.Where(item => item.Status == ParticipantStatus.CONFIRMED)) participant.Complete();
        RenewConcurrencyToken();
    }
}
