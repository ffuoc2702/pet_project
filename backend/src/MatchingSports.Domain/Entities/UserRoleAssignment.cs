using MatchingSports.Shared;

namespace MatchingSports.Domain.Entities;

public sealed class UserRoleAssignment(Guid userId, IReadOnlyCollection<string> roles,
    IReadOnlyCollection<string>? permissions = null, string? concurrencyStamp = null,
    IReadOnlyCollection<Guid>? roleIds = null)
{
    public Guid UserId { get; } = userId;
    public IReadOnlyCollection<string> Roles { get; private set; } = Array.AsReadOnly(roles.ToArray());
    public IReadOnlyCollection<Guid> RoleIds { get; private set; } = Array.AsReadOnly((roleIds ?? []).ToArray());
    public string? ConcurrencyStamp { get; private set; } = concurrencyStamp;
    public string? OriginalConcurrencyStamp { get; } = concurrencyStamp;

    public IReadOnlyCollection<string> Permissions { get; } = Array.AsReadOnly((permissions ?? []).Distinct(StringComparer.Ordinal).ToArray());
    public bool HasPermission(string code) => Permissions.Contains(code, StringComparer.Ordinal);
    public bool CanAssignUserRoles => HasPermission(PermissionCodes.AssignUserRole);

    public void ReplaceRoles(IEnumerable<Role> roles)
    {
        ArgumentNullException.ThrowIfNull(roles);
        var requested = roles.ToArray();
        if (requested.Any(role => role is null || !role.IsActive))
            throw new ArgumentException("Only active roles can be assigned to a user.", nameof(roles));
        RoleIds = Array.AsReadOnly(requested.Select(role => role.Id).Distinct().Order().ToArray());
        Roles = Array.AsReadOnly(requested.Select(role => role.Name!).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray());
        ConcurrencyStamp = Guid.NewGuid().ToString();
    }
}
