using MatchingSports.Domain.Enums;

namespace MatchingSports.Domain.Entities;

public sealed class ListingRequest : ConcurrentAuditableEntity
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public Guid? ApplicantUserId { get; set; }
    public Guid? ApplicantTeamId { get; set; }
    public RequestType Type { get; set; }
    public short RequestedSlots { get; set; } = 1;
    public string? Message { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.PENDING;
    public Guid? DecidedByUserId { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public DateTimeOffset? WithdrawnAt { get; set; }
    public string? RejectionReason { get; set; }

    public Listing Listing { get; set; } = null!;
    public User? ApplicantUser { get; set; }
    public Team? ApplicantTeam { get; set; }
    public User? DecidedByUser { get; set; }
    public ICollection<MatchParticipant> MatchParticipants { get; set; } = [];

    internal static ListingRequest Create(Guid listingId, Guid? userId, Guid? teamId, string? message)
    {
        MatchingRules.Require(message is null || message.Length <= 1000, "Lời nhắn không được vượt quá 1000 ký tự.");
        return new ListingRequest
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            ApplicantUserId = userId,
            ApplicantTeamId = teamId,
            Type = userId is not null ? RequestType.PLAYER : RequestType.OPPONENT,
            Message = MatchingSports.Shared.StringNormalizer.StartEndSpaceTrim(message)
        };
    }
    internal void RequirePending() => MatchingRules.Require(Status == RequestStatus.PENDING, "Yêu cầu không còn chờ xử lý.");
    internal void Accept(Guid actorId, DateTimeOffset now)
    { RequirePending(); Status = RequestStatus.ACCEPTED; DecidedByUserId = actorId; DecidedAt = now; }
    internal void Reject(Guid actorId, string reason, DateTimeOffset now)
    {
        RequirePending();
        var normalizedReason = MatchingRules.Text(reason, 1000, "Lý do từ chối");
        Status = RequestStatus.REJECTED; DecidedByUserId = actorId; DecidedAt = now; RejectionReason = normalizedReason;
    }
    internal void Withdraw(DateTimeOffset now) { Status = RequestStatus.WITHDRAWN; WithdrawnAt = now; }
    internal void Expire(DateTimeOffset now) { Status = RequestStatus.EXPIRED; DecidedAt = now; }
    internal void Cancel(DateTimeOffset now) { Status = RequestStatus.CANCELLED; DecidedAt = now; }
}
