namespace MatchingSports.Application.Features.Role;

public sealed record UserRoleDto(Guid UserId, IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<Guid> RoleIds, string? ConcurrencyStamp);
