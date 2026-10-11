using MatchingSports.Application.Common;
using MatchingSports.Domain.Enums;

namespace MatchingSports.Application.Features.User;

public sealed record UserListRoleDto(Guid Id, string Name, bool IsActive);

public sealed record UserListDto : BaseDto
{
    public Guid Id { get; init; }
    public string? DisplayName { get; init; }
    public string? Email { get; init; }
    public bool EmailConfirmed { get; init; }
    public UserStatus Status { get; init; }
    public DateTimeOffset? LastLoginAt { get; init; }
    public IReadOnlyCollection<UserListRoleDto> Roles { get; init; } = [];
}
