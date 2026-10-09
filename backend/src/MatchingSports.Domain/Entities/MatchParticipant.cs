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

    internal static MatchParticipant Create(Guid matchId, Guid? userId, Guid? teamId, Guid? requestId, short sideNo, ParticipantRole role)
    {
        MatchingRules.Require(matchId != Guid.Empty && (userId is not null) != (teamId is not null)
            && userId != Guid.Empty && teamId != Guid.Empty && sideNo is 1 or 2 && Enum.IsDefined(role), "Người tham gia không hợp lệ.");
        return new MatchParticipant
        {
            Id = Guid.NewGuid(),
            MatchId = matchId,
            UserId = userId,
            TeamId = teamId,
            SourceRequestId = requestId,
            SideNo = sideNo,
            Role = role
        };
    }
    internal void Rejoin(Guid requestId) { SourceRequestId = requestId; Status = ParticipantStatus.CONFIRMED; }
    internal void Cancel() => Status = ParticipantStatus.CANCELLED;
    internal void Complete() => Status = ParticipantStatus.COMPLETED;
}
