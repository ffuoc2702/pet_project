using MatchingSports.Application;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Role;
using MatchingSports.Domain.Entities;
using MatchingSports.Shared;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace MatchingSports.Tests;

public sealed class RolePermissionTests
{
    [Fact]
    public void GrantPermissionIsIdempotentAndRevokeRemovesOnlyRequestedPermission()
    {
        var role = Role.Create(RoleNames.Admin);
        var read = Permission.Create(PermissionCodes.ReadRoles, "Xem role");
        var assign = Permission.Create(PermissionCodes.AssignUserRole, "Gán role");

        role.GrantPermission(read);
        role.GrantPermission(read);
        role.GrantPermission(assign);
        Assert.Equal(2, role.Permissions.Count);
        Assert.All(role.Permissions, item => Assert.Equal(role.Id, item.RoleId));

        role.RevokePermission(read.Id);

        var remaining = Assert.Single(role.Permissions);
        Assert.Equal(assign.Id, remaining.PermissionId);
        Assert.Same(assign, remaining.Permission);
    }

    [Fact]
    public void CreatePermissionNormalizesCodeAndDisplayName()
    {
        var permission = Permission.Create("  ROLES.READ  ", "  Xem role  ");

        Assert.Equal(PermissionCodes.ReadRoles, permission.Code);
        Assert.Equal("Xem role", permission.Name);
        Assert.NotEqual(Guid.Empty, permission.Id);
    }

    [Theory]
    [InlineData("", "Xem role")]
    [InlineData("roles read", "Xem role")]
    [InlineData("roles.read", " ")]
    public void CreatePermissionRejectsInvalidValues(string code, string name) =>
        Assert.ThrowsAny<ArgumentException>(() => Permission.Create(code, name));

    [Fact]
    public async Task GetRolesMapsPersistedIdentityAndPermissionsToDtos()
    {
        var role = Role.Create(RoleNames.Admin);
        var permission = Permission.Create(PermissionCodes.ReadRoles, "Xem role");
        role.GrantPermission(permission);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<IRoleRepository>(new StubRoleRepository(role));
        services.AddSingleton<IUserRoleRepository>(new StubUserRepository());
        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new GetRolesQuery { ActorUserId = Guid.NewGuid() }, CancellationToken.None);

        var dto = Assert.Single(result.Items);
        Assert.Equal(role.Id, dto.Id);
        Assert.Equal(RoleNames.Admin, dto.Name);
        Assert.Equal(permission.Id, Assert.Single(dto.Permissions).Id);
    }

    private sealed class StubUserRepository : IUserRoleRepository
    {
        public Task<UserRoleAssignment?> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<UserRoleAssignment?>(new(userId, ["ACCESS_MANAGER"], [PermissionCodes.ListRoles]));
        public Task ReplaceUserRolesAsync(UserRoleAssignment assignment, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class StubRoleRepository(Role role) : IRoleRepository
    {
        public Task<PaginatedResult<Role>> ListAsync(GetRolesQuery query, CancellationToken cancellationToken) =>
            Task.FromResult(new PaginatedResult<Role>([role], query.PageNumber, query.PageSize, 1));

        public Task<IReadOnlyCollection<Role>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<Role>>([]);
        public Task<bool> ExistsByNormalizedNameAsync(string name, Guid? excludedRoleId, CancellationToken cancellationToken) => Task.FromResult(false);
        public void Add(Role newRole) => throw new NotSupportedException();

        public Task<Role?> GetByIdWithPermissionsAsync(Guid roleId, CancellationToken cancellationToken) => GetForUpdateAsync(roleId, cancellationToken);
        public Task<Role?> GetForUpdateAsync(Guid roleId, CancellationToken cancellationToken) =>
            Task.FromResult<Role?>(role.Id == roleId ? role : null);
    }
}
