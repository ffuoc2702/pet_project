using MatchingSports.Domain.Enums;

namespace MatchingSports.Domain.Entities;

public sealed class MatchParticipant : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? TeamId { get; set; }
    public Guid? SourceRequestId { get; set; }
    public short SideNo { get; set; }
    public ParticipantRole Role { get; set; }
    public ParticipantStatus Status { get; set; } = ParticipantStatus.CONFIRMED;

    public Match Match { get; set; } = null!;
    public User? User { get; set; }
    public Team? Team { get; set; }
    public ListingRequest? SourceRequest { get; set; }
}
