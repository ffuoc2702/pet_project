using MatchingSports.Application.Common;

namespace MatchingSports.Application.Features.Role;

public sealed record PermissionDto(Guid Id, string Code, string Name);

public sealed record RoleDto(Guid Id, string Name, string? Description, bool IsActive,
    bool IsDefaultForRegistration, string ConcurrencyStamp, IReadOnlyCollection<PermissionDto> Permissions);
public sealed record RolePermissionMatrixDto(IReadOnlyCollection<PermissionDto> Permissions, PaginatedResult<RoleDto> Roles);
