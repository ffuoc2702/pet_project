using MatchingSports.Domain.Enums;

namespace MatchingSports.Domain.Entities;

public sealed class ListingRequest : IHasConcurrencyToken
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
    public Guid ConcurrencyToken { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Listing Listing { get; set; } = null!;
    public User? ApplicantUser { get; set; }
    public Team? ApplicantTeam { get; set; }
    public User? DecidedByUser { get; set; }
    public ICollection<MatchParticipant> MatchParticipants { get; set; } = [];
}
