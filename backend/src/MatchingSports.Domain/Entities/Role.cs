using MatchingSports.Domain.ValueObjects;
using MatchingSports.Shared;
using Microsoft.AspNetCore.Identity;

namespace MatchingSports.Domain.Entities;

public sealed class Role : IdentityRole<Guid>, IAuditableEntity
{
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    private readonly List<RolePermission> _permissions = [];

    private Role() { }

    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();
    public bool PermissionsInitialized { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsDefaultForRegistration { get; private set; }
    public bool RegistrationDefaultInitialized { get; private set; }
    public string? SeedKey { get; private set; }
    public bool BootstrapCompleted { get; private set; }

    public void CompleteBootstrap() => BootstrapCompleted = true;

    public void InitializeSeedIdentity(string seedKey)
    {
        if (seedKey is null)
            throw new ArgumentNullException(nameof(seedKey), "Định danh khởi tạo không được để trống.");
        if (string.IsNullOrWhiteSpace(seedKey))
            throw new ArgumentException("Định danh khởi tạo không được để trống.", nameof(seedKey));
        if (SeedKey is not null && SeedKey != seedKey)
            throw new InvalidOperationException("Không thể thay đổi định danh khởi tạo của vai trò.");
        SeedKey = seedKey;
    }

    public static Role Create(string name, string? description = null, bool isDefaultForRegistration = false)
    {
        var role = new Role { Id = Guid.NewGuid() };
        role.UpdateDetails(name, description, true, isDefaultForRegistration);
        return role;
    }

    public void UpdateDetails(string name, string? description, bool isActive, bool isDefaultForRegistration)
        => UpdateDetails(RoleDetails.Create(name, description, isActive, isDefaultForRegistration));

    public void UpdateDetails(RoleDetails details)
    {
        if (details is null)
            throw new ArgumentNullException(nameof(details), "Thông tin chi tiết không được là null.");
        Name = details.Name;
        NormalizedName = details.Name;
        Description = details.Description;
        IsActive = details.IsActive;
        IsDefaultForRegistration = details.IsDefaultForRegistration;
        RegistrationDefaultInitialized = true;
        ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    public void InitializeRegistrationDefault(bool isDefaultForRegistration)
    {
        if (RegistrationDefaultInitialized)
            return;
        IsDefaultForRegistration = IsActive && isDefaultForRegistration;
        RegistrationDefaultInitialized = true;
        ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    public void GrantPermission(Permission permission)
    {
        if (permission is null)
            throw new ArgumentNullException(nameof(permission), "Quyền không được là null.");
        if (_permissions.Any(item => item.PermissionId == permission.Id))
            return;

        _permissions.Add(new RolePermission(this, permission));
        ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    public void RevokePermission(Guid permissionId)
    {
        if (_permissions.RemoveAll(item => item.PermissionId == permissionId) > 0)
            ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    public void InitializeDefaultPermissions(IEnumerable<Permission> permissions)
    {
        if (PermissionsInitialized)
            return;
        ReplacePermissions(permissions.Concat(_permissions.Select(item => item.Permission)).ToArray());
        PermissionsInitialized = true;
        ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    public void ReplacePermissions(IEnumerable<Permission> permissions)
    {
        if (permissions is null)
            throw new ArgumentNullException(nameof(permissions), "Danh sách quyền không được là null.");
        var requested = permissions.ToArray();
        if (requested.Any(permission => permission is null))
            throw new ArgumentException("Danh sách quyền không được chứa phần tử null.", nameof(permissions));
        var ids = requested.Select(permission => permission.Id).ToHashSet();
        foreach (var removed in _permissions.Where(item => !ids.Contains(item.PermissionId)).ToArray())
            RevokePermission(removed.PermissionId);
        foreach (var permission in requested)
            GrantPermission(permission);
    }
}
