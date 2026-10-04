namespace MatchingSports.Domain.Entities;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711", Justification = "Role-to-permission entity; unrelated to legacy Code Access Security.")]
public sealed class RolePermission
{
    private RolePermission() { }

    internal RolePermission(Role role, Permission permission)
    {
        RoleId = role.Id;
        PermissionId = permission.Id;
        Role = role;
        Permission = permission;
    }

    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }
    public Role Role { get; private set; } = null!;
    public Permission Permission { get; private set; } = null!;
}
