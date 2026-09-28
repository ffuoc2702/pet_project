using MatchingSports.Domain.Enums;

namespace MatchingSports.Domain.Entities;

public sealed class TeamMember
{
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }
    public TeamRole Role { get; set; } = TeamRole.MEMBER;
    public MemberStatus Status { get; set; } = MemberStatus.ACTIVE;
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? LeftAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Team Team { get; set; } = null!;
    public User User { get; set; } = null!;
}
