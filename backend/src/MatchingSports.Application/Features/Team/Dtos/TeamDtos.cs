using MatchingSports.Application.Common;
using MatchingSports.Domain.Enums;

namespace MatchingSports.Application.Features.Team;

public sealed record TeamDto : ConcurrentBaseDto
{
    public Guid Id { get; init; }
    public Guid SportId { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid CaptainUserId { get; init; }
    public string? Description { get; init; }
    public string? HomeArea { get; init; }
    public string? AvatarUrl { get; init; }
    public TeamStatus Status { get; init; }
    public DateTimeOffset? DeletedAt { get; init; }
}
public sealed record TeamMemberDto : BaseDto
{
    public Guid TeamId { get; init; }
    public Guid UserId { get; init; }
    public TeamRole Role { get; init; }
    public MemberStatus Status { get; init; }
    public DateTimeOffset JoinedAt { get; init; }
    public DateTimeOffset? LeftAt { get; init; }
}
public sealed record TeamInvitationDto : ConcurrentBaseDto
{
    public Guid Id { get; init; }
    public Guid TeamId { get; init; }
    public Guid UserId { get; init; }
    public Guid InviterUserId { get; init; }
    public TeamRequestStatus Status { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset? RespondedAt { get; init; }
}
public sealed record TeamJoinRequestDto : ConcurrentBaseDto
{
    public Guid Id { get; init; }
    public Guid TeamId { get; init; }
    public Guid UserId { get; init; }
    public string? Message { get; init; }
    public TeamRequestStatus Status { get; init; }
    public Guid? ReviewedBy { get; init; }
    public DateTimeOffset? RespondedAt { get; init; }
}
