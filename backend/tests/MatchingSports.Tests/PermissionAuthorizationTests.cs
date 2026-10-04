using MatchingSports.Application;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Role;
using MatchingSports.Domain.Entities;
using MatchingSports.Infrastructure.Persistence.Seed;
using MatchingSports.Shared;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace MatchingSports.Tests;

public sealed class PermissionAuthorizationTests
{
    [Fact]
    public void SeedCatalogCoversEveryConstantAndUserReceivesNoAdministrativePermission()
    {
        var codes = typeof(PermissionCodes).GetFields().Select(field => (string)field.GetRawConstantValue()!).ToHashSet();
        Assert.Equal(codes.Count, InitialPermissionCatalog.All.Count);
        Assert.True(codes.SetEquals(InitialPermissionCatalog.GetDefaultPermissionCodes(RoleNames.Admin)));
        var userCodes = InitialPermissionCatalog.GetDefaultPermissionCodes(RoleNames.User).ToHashSet();
        Assert.Contains(PermissionCodes.ReadSports, userCodes);
        Assert.Contains(PermissionCodes.UpdateOwnProfile, userCodes);
        Assert.DoesNotContain(PermissionCodes.AssignUserRole, userCodes);
        Assert.DoesNotContain(PermissionCodes.UpdateRolePermissions, userCodes);
        Assert.DoesNotContain(PermissionCodes.SuspendUsers, userCodes);
    }

    [Fact]
    public void SeedingAgainDoesNotRestoreRevokedPermission()
    {
        var role = Role.Create(RoleNames.User);
        var permission = Permission.Create(PermissionCodes.UpdateOwnProfile, "Cập nhật hồ sơ");
        role.InitializeDefaultPermissions([permission]);
        role.RevokePermission(permission.Id);

        role.InitializeDefaultPermissions([permission]);

        Assert.True(role.PermissionsInitialized);
        Assert.Empty(role.Permissions);
    }

    [Fact]
    public void ReplacingPermissionsPreservesRetainedLinksAndRotatesConcurrencyStamp()
    {
        var role = Role.Create(RoleNames.User);
        var read = Permission.Create(PermissionCodes.ReadOwnProfile, "Xem hồ sơ");
        var write = Permission.Create(PermissionCodes.UpdateOwnProfile, "Cập nhật hồ sơ");
        role.InitializeDefaultPermissions([read, write]);
        var retainedLink = role.Permissions.Single(item => item.PermissionId == read.Id);
        var previousStamp = role.ConcurrencyStamp;

        role.ReplacePermissions([read, read]);

        Assert.Same(retainedLink, Assert.Single(role.Permissions));
        Assert.NotEqual(previousStamp, role.ConcurrencyStamp);
    }

    [Fact]
    public async Task AdministratorWithoutPermissionCannotChangeRolePermissions()
    {
        var actor = new UserRoleAssignment(Guid.NewGuid(), [RoleNames.Admin]);
        var repository = new StubRepository(actor, Role.Create(RoleNames.User));
        using var provider = CreateProvider(repository);
        var mediator = provider.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<ForbiddenException>(() => mediator.Send(
            new UpdateRolePermissionsCommand(actor.UserId, repository.Role.Id, [], repository.Role.ConcurrencyStamp!), CancellationToken.None));

        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task PermissionHolderCanReplaceRolePermissionsAndCommitOnce()
    {
        var actor = new UserRoleAssignment(Guid.NewGuid(), [RoleNames.Admin], [PermissionCodes.UpdateRolePermissions]);
        var role = Role.Create(RoleNames.User);
        var permission = Permission.Create(PermissionCodes.ReadSports, "Xem môn");
        var repository = new StubRepository(actor, role, permission);
        using var provider = CreateProvider(repository);

        var result = await provider.GetRequiredService<IMediator>().Send(
            new UpdateRolePermissionsCommand(actor.UserId, role.Id, [permission.Code], role.ConcurrencyStamp!), CancellationToken.None);

        Assert.Equal(permission.Code, Assert.Single(result.Permissions).Code);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task UnknownPermissionDoesNotMutateRoleOrCommit()
    {
        var actor = new UserRoleAssignment(Guid.NewGuid(), [RoleNames.Admin], [PermissionCodes.UpdateRolePermissions]);
        var role = Role.Create(RoleNames.User);
        var permission = Permission.Create(PermissionCodes.ReadSports, "Xem môn");
        role.GrantPermission(permission);
        var repository = new StubRepository(actor, role, permission);
        using var provider = CreateProvider(repository);

        var exception = await Assert.ThrowsAsync<RequestValidationException>(() => provider.GetRequiredService<IMediator>().Send(
            new UpdateRolePermissionsCommand(actor.UserId, role.Id, ["unknown.permission"], role.ConcurrencyStamp!), CancellationToken.None));

        Assert.Equal(ErrorCodes.InvalidPermission, exception.Code);
        Assert.Equal(permission.Id, Assert.Single(role.Permissions).PermissionId);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task StalePermissionMatrixDoesNotMutateRoleOrCommit()
    {
        var actor = new UserRoleAssignment(Guid.NewGuid(), ["ACCESS_MANAGER"], [PermissionCodes.UpdateRolePermissions]);
        var role = Role.Create("SUPPORT");
        var permission = Permission.Create(PermissionCodes.ReadSports, "Xem môn");
        role.GrantPermission(permission);
        var repository = new StubRepository(actor, role, permission);
        using var provider = CreateProvider(repository);
        var error = await Assert.ThrowsAsync<ConflictException>(() => provider.GetRequiredService<IMediator>().Send(
            new UpdateRolePermissionsCommand(actor.UserId, role.Id, [], "stale"), CancellationToken.None));
        Assert.Equal(ErrorCodes.ConcurrencyConflict, error.Code);
        Assert.Single(role.Permissions);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task EmptyPermissionListRevokesEveryGrant()
    {
        var actor = new UserRoleAssignment(Guid.NewGuid(), ["ACCESS_MANAGER"], [PermissionCodes.UpdateRolePermissions]);
        var role = Role.Create("SUPPORT");
        role.GrantPermission(Permission.Create(PermissionCodes.ReadSports, "Xem môn"));
        var repository = new StubRepository(actor, role);
        using var provider = CreateProvider(repository);
        var result = await provider.GetRequiredService<IMediator>().Send(new UpdateRolePermissionsCommand(actor.UserId,
            role.Id, [], role.ConcurrencyStamp!), CancellationToken.None);
        Assert.Empty(result.Permissions);
        Assert.Equal(1, repository.SaveCount);
    }

    private static ServiceProvider CreateProvider(StubRepository repository)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<IRoleRepository>(repository);
        services.AddSingleton<IPermissionRepository>(repository);
        services.AddSingleton<IUserRoleRepository>(repository);
        services.AddSingleton<IAuthorizationAuditRepository>(repository);
        services.AddSingleton<IUnitOfWork>(repository);
        return services.BuildServiceProvider();
    }

    private sealed class StubRepository(UserRoleAssignment actor, Role role, params Permission[] permissions)
        : IRoleRepository, IPermissionRepository, IUserRoleRepository, IAuthorizationAuditRepository, IUnitOfWork
    {
        public void Add(AuthorizationAuditEntry entry) { }
        public Role Role { get; } = role;
        public int SaveCount { get; private set; }

        public Task<IReadOnlyCollection<Role>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<Role>>([]);
        public Task<bool> ExistsByNormalizedNameAsync(string name, Guid? excludedRoleId, CancellationToken cancellationToken) => Task.FromResult(false);
        public void Add(Role newRole) => throw new NotSupportedException();

        public Task<Role?> GetByIdWithPermissionsAsync(Guid roleId, CancellationToken cancellationToken) => GetForUpdateAsync(roleId, cancellationToken);
        public Task<Role?> GetForUpdateAsync(Guid roleId, CancellationToken cancellationToken) =>
            Task.FromResult<Role?>(roleId == Role.Id ? Role : null);
        public Task<PaginatedResult<Role>> ListAsync(BaseQuery query, CancellationToken cancellationToken) =>
            Task.FromResult(new PaginatedResult<Role>([Role], query.PageNumber, query.PageSize, 1));
        public Task<Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(permissions.SingleOrDefault(permission => permission.Id == id));
        Task<PaginatedResult<Permission>> IPermissionRepository.ListAsync(BaseQuery query, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<IReadOnlyCollection<Permission>> GetPermissionsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<Permission>>(permissions);
        public Task<IReadOnlyCollection<Permission>> GetByCodesAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<Permission>>(permissions.Where(permission => codes.Contains(permission.Code)).ToArray());
        public Task<UserRoleAssignment?> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(userId == actor.UserId ? actor : null);
        public Task ReplaceUserRolesAsync(UserRoleAssignment assignment, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
