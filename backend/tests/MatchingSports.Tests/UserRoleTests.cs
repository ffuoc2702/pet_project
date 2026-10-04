using MatchingSports.Application;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Role;
using MatchingSports.Domain.Entities;
using MatchingSports.Shared;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace MatchingSports.Tests;

public sealed class UserRoleTests
{
    [Fact]
    public void ReplaceRolesSupportsCustomRolesAndDeduplicatesMembership()
    {
        var moderator = Role.Create("moderator");
        var support = Role.Create("support");
        var assignment = new UserRoleAssignment(Guid.NewGuid(), [RoleNames.User], concurrencyStamp: "original");

        assignment.ReplaceRoles([moderator, support, moderator]);

        Assert.Equal(["MODERATOR", "SUPPORT"], assignment.Roles);
        Assert.Equal(2, assignment.RoleIds.Count);
        Assert.NotEqual("original", assignment.ConcurrencyStamp);
        Assert.Equal("original", assignment.OriginalConcurrencyStamp);
    }

    [Fact]
    public void InactiveRoleIsRejectedBeforeChangingMembership()
    {
        var role = Role.Create("moderator");
        role.UpdateDetails(role.Name!, null, false, false);
        var assignment = new UserRoleAssignment(Guid.NewGuid(), [RoleNames.User]);

        Assert.Throws<ArgumentException>(() => assignment.ReplaceRoles([role]));

        Assert.Equal([RoleNames.User], assignment.Roles);
    }

    [Theory]
    [InlineData(RoleNames.Admin)]
    [InlineData(RoleNames.User)]
    [InlineData("SUPPORT")]
    public async Task PermissionHolderCanAssignMultipleCustomRoles(string actorRole)
    {
        var actor = new UserRoleAssignment(Guid.NewGuid(), [actorRole], [PermissionCodes.AssignUserRole]);
        var target = new UserRoleAssignment(Guid.NewGuid(), [RoleNames.User], concurrencyStamp: "version");
        var roles = new[] { Role.Create("moderator"), Role.Create("support") };
        var repository = new StubRepository([actor, target], roles);
        using var provider = CreateProvider(repository);

        var result = await provider.GetRequiredService<IMediator>().Send(
            new ReplaceUserRolesCommand(actor.UserId, target.UserId, roles.Select(role => role.Id).ToArray(), "version"),
            CancellationToken.None);

        Assert.Equal(["MODERATOR", "SUPPORT"], result.Roles);
        Assert.Equal(1, repository.SaveCount);
        Assert.Equal(1, repository.ReplaceCount);
        Assert.Single(repository.AuditEntries);
    }

    [Fact]
    public async Task AdministratorWithoutPermissionCannotAssignRoles()
    {
        var actor = new UserRoleAssignment(Guid.NewGuid(), [RoleNames.Admin]);
        var repository = new StubRepository([actor], []);
        using var provider = CreateProvider(repository);

        await Assert.ThrowsAsync<ForbiddenException>(() => provider.GetRequiredService<IMediator>().Send(
            new ReplaceUserRolesCommand(actor.UserId, Guid.NewGuid(), [], "version"), CancellationToken.None));

        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task PermissionHolderCanRemoveOwnRolesWithoutHardCodedRoleRule()
    {
        var actor = new UserRoleAssignment(Guid.NewGuid(), ["SUPPORT"], [PermissionCodes.AssignUserRole], "version");
        var repository = new StubRepository([actor], []);
        using var provider = CreateProvider(repository);

        var result = await provider.GetRequiredService<IMediator>().Send(
            new ReplaceUserRolesCommand(actor.UserId, actor.UserId, [], "version"), CancellationToken.None);

        Assert.Empty(result.RoleIds);
        Assert.Empty(result.Roles);
        Assert.Equal(1, repository.SaveCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnknownOrInactiveRoleDoesNotWrite(bool inactive)
    {
        var role = Role.Create("moderator");
        if (inactive)
            role.UpdateDetails(role.Name!, null, false, false);
        var actor = new UserRoleAssignment(Guid.NewGuid(), ["SUPPORT"], [PermissionCodes.AssignUserRole]);
        var target = new UserRoleAssignment(Guid.NewGuid(), [RoleNames.User], concurrencyStamp: "version");
        var repository = new StubRepository([actor, target], inactive ? [role] : []);
        using var provider = CreateProvider(repository);

        var error = await Assert.ThrowsAsync<RequestValidationException>(() => provider.GetRequiredService<IMediator>().Send(
            new ReplaceUserRolesCommand(actor.UserId, target.UserId, [role.Id], "version"), CancellationToken.None));

        Assert.Equal(ErrorCodes.InvalidRole, error.Code);
        Assert.Equal(0, repository.ReplaceCount);
        Assert.Equal(0, repository.SaveCount);
        Assert.Empty(repository.AuditEntries);
    }

    [Fact]
    public async Task StaleUserRolesVersionDoesNotWrite()
    {
        var actor = new UserRoleAssignment(Guid.NewGuid(), ["SUPPORT"], [PermissionCodes.AssignUserRole]);
        var target = new UserRoleAssignment(Guid.NewGuid(), [RoleNames.User], concurrencyStamp: "current");
        var repository = new StubRepository([actor, target], []);
        using var provider = CreateProvider(repository);

        var error = await Assert.ThrowsAsync<ConflictException>(() => provider.GetRequiredService<IMediator>().Send(
            new ReplaceUserRolesCommand(actor.UserId, target.UserId, [], "old"), CancellationToken.None));

        Assert.Equal(ErrorCodes.ConcurrencyConflict, error.Code);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task MissingUserDoesNotCommit()
    {
        var actor = new UserRoleAssignment(Guid.NewGuid(), ["SUPPORT"], [PermissionCodes.AssignUserRole]);
        var repository = new StubRepository([actor], []);
        using var provider = CreateProvider(repository);

        var error = await Assert.ThrowsAsync<NotFoundException>(() => provider.GetRequiredService<IMediator>().Send(
            new ReplaceUserRolesCommand(actor.UserId, Guid.NewGuid(), [], "version"), CancellationToken.None));

        Assert.Equal(ErrorCodes.UserNotFound, error.Code);
        Assert.Equal(0, repository.SaveCount);
    }

    private static ServiceProvider CreateProvider(StubRepository repository)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<IUserRoleRepository>(repository);
        services.AddSingleton<IRoleRepository>(repository);
        services.AddSingleton<IAuthorizationAuditRepository>(repository);
        services.AddSingleton<IUnitOfWork>(repository);
        return services.BuildServiceProvider();
    }

    private sealed class StubRepository(UserRoleAssignment[] users, Role[] roles)
        : IUserRoleRepository, IRoleRepository, IAuthorizationAuditRepository, IUnitOfWork
    {
        public int ReplaceCount { get; private set; }
        public int SaveCount { get; private set; }
        public List<AuthorizationAuditEntry> AuditEntries { get; } = [];
        public Task<UserRoleAssignment?> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(users.SingleOrDefault(user => user.UserId == userId));
        public Task ReplaceUserRolesAsync(UserRoleAssignment assignment, CancellationToken cancellationToken)
        {
            ReplaceCount++;
            return Task.CompletedTask;
        }
        public Task<IReadOnlyCollection<Role>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<Role>>(roles.Where(role => ids.Contains(role.Id)).ToArray());
        public Task<PaginatedResult<Role>> ListAsync(BaseQuery query, CancellationToken cancellationToken) =>
            Task.FromResult(new PaginatedResult<Role>(roles.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToArray(), query.PageNumber, query.PageSize, roles.Length));
        public Task<Role?> GetByIdWithPermissionsAsync(Guid roleId, CancellationToken cancellationToken) => GetForUpdateAsync(roleId, cancellationToken);
        public Task<Role?> GetForUpdateAsync(Guid roleId, CancellationToken cancellationToken) => Task.FromResult<Role?>(null);
        public Task<bool> ExistsByNormalizedNameAsync(string name, Guid? excludedRoleId, CancellationToken cancellationToken) => Task.FromResult(false);
        public void Add(Role role) => throw new NotSupportedException();
        public void Add(AuthorizationAuditEntry entry) => AuditEntries.Add(entry);
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
