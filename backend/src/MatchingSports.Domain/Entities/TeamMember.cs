using MatchingSports.Domain.Enums;

namespace MatchingSports.Domain.Entities;

public sealed class TeamMember : AuditableEntity
{
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }
    public TeamRole Role { get; set; } = TeamRole.MEMBER;
    public MemberStatus Status { get; set; } = MemberStatus.ACTIVE;
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? LeftAt { get; set; }

    public static TeamMember Join(Guid teamId, Guid userId, TeamRole role, DateTimeOffset now)
    {
        TeamRules.Require(teamId != Guid.Empty && userId != Guid.Empty && Enum.IsDefined(role), "Membership không hợp lệ.");
        return new() { TeamId = teamId, UserId = userId, Role = role, JoinedAt = now };
    }

    internal void Rejoin(DateTimeOffset now)
    {
        Status = MemberStatus.ACTIVE; Role = TeamRole.MEMBER; JoinedAt = now; LeftAt = null;
    }
    internal void ChangeRole(TeamRole role) => Role = role;
    internal void Leave(MemberStatus status, DateTimeOffset now)
    {
        Status = status; LeftAt = now; Role = TeamRole.MEMBER;
    }

    public Team Team { get; set; } = null!;
    public User User { get; set; } = null!;
}
