using MatchingSports.Api.Controllers;
using MatchingSports.Application;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Permission;
using MatchingSports.Application.Features.Role;
using MatchingSports.Application.Features.Sport;
using MatchingSports.Domain.Entities;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace MatchingSports.Tests;

public sealed class ListReadPermissionTests
{
    [Theory]
    [InlineData(PermissionCodes.ReadSports, PermissionCodes.ListSports)]
    [InlineData(PermissionCodes.ReadRoles, PermissionCodes.ListRoles)]
    [InlineData(PermissionCodes.ReadPermissions, PermissionCodes.ListPermissions)]
    public async Task ReadDoesNotAuthorizeListAtHandlerBoundary(string readCode, string listCode)
    {
        using var provider = CreateProvider(readCode);
        var mediator = provider.GetRequiredService<IMediator>();
        Task operation = listCode switch
        {
            PermissionCodes.ListSports => mediator.Send(new GetSportsQuery { ActorUserId = Guid.NewGuid() }),
            PermissionCodes.ListRoles => mediator.Send(new GetRolesQuery { ActorUserId = Guid.NewGuid() }),
            _ => mediator.Send(new GetPermissionsQuery { ActorUserId = Guid.NewGuid() })
        };
        var error = await Assert.ThrowsAsync<ForbiddenException>(() => operation);
        Assert.Equal(ErrorCodes.Forbidden, error.Code);
    }

    [Theory]
    [InlineData(PermissionCodes.ListSports, PermissionCodes.ReadSports)]
    [InlineData(PermissionCodes.ListRoles, PermissionCodes.ReadRoles)]
    [InlineData(PermissionCodes.ListPermissions, PermissionCodes.ReadPermissions)]
    public async Task ListDoesNotAuthorizeReadAtHandlerBoundary(string listCode, string readCode)
    {
        using var provider = CreateProvider(listCode);
        var mediator = provider.GetRequiredService<IMediator>();
        Task operation = readCode switch
        {
            PermissionCodes.ReadSports => mediator.Send(new GetSportByIdQuery(Guid.NewGuid(), Guid.NewGuid())),
            PermissionCodes.ReadRoles => mediator.Send(new GetRoleQuery(Guid.NewGuid(), Guid.NewGuid())),
            _ => mediator.Send(new GetPermissionQuery(Guid.NewGuid(), Guid.NewGuid()))
        };
        await Assert.ThrowsAsync<ForbiddenException>(() => operation);
    }

    [Theory]
    [InlineData(PermissionCodes.ReadSports, ErrorCodes.SportNotFound)]
    [InlineData(PermissionCodes.ReadRoles, ErrorCodes.RoleNotFound)]
    [InlineData(PermissionCodes.ReadPermissions, ErrorCodes.PermissionNotFound)]
    public async Task AuthorizedDetailQueryReportsMissingResource(string readCode, string expectedCode)
    {
        using var provider = CreateProvider(readCode);
        var mediator = provider.GetRequiredService<IMediator>();
        Task operation = readCode switch
        {
            PermissionCodes.ReadSports => mediator.Send(new GetSportByIdQuery(Guid.NewGuid(), Guid.NewGuid())),
            PermissionCodes.ReadRoles => mediator.Send(new GetRoleQuery(Guid.NewGuid(), Guid.NewGuid())),
            _ => mediator.Send(new GetPermissionQuery(Guid.NewGuid(), Guid.NewGuid()))
        };
        var error = await Assert.ThrowsAsync<NotFoundException>(() => operation);
        Assert.Equal(expectedCode, error.Code);
    }

    [Theory]
    [InlineData(typeof(SportController), nameof(SportController.GetSports), PermissionCodes.ListSports)]
    [InlineData(typeof(SportController), nameof(SportController.GetSport), PermissionCodes.ReadSports)]
    [InlineData(typeof(SportController), nameof(SportController.CreateSport), PermissionCodes.CreateSports)]
    [InlineData(typeof(SportController), nameof(SportController.UpdateSport), PermissionCodes.UpdateSports)]
    [InlineData(typeof(SportController), nameof(SportController.DeactivateSport), PermissionCodes.DeactivateSports)]
    [InlineData(typeof(RoleController), nameof(RoleController.GetRoles), PermissionCodes.ListRoles)]
    [InlineData(typeof(UsersController), nameof(UsersController.GetUsers), PermissionCodes.ListUsers)]
    [InlineData(typeof(RoleController), nameof(RoleController.GetRole), PermissionCodes.ReadRoles)]
    [InlineData(typeof(PermissionController), nameof(PermissionController.GetPermissions), PermissionCodes.ListPermissions)]
    [InlineData(typeof(PermissionController), nameof(PermissionController.GetPermission), PermissionCodes.ReadPermissions)]
    public void EndpointPolicyDistinguishesCollectionAndDetail(Type controller, string action, string permissionCode)
    {
        var policies = controller.GetMethod(action)!.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().Select(attribute => attribute.Policy).ToArray();
        Assert.Equal(permissionCode, Assert.Single(policies));
    }

    private static ServiceProvider CreateProvider(string permissionCode)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        var repository = new StubRepository(permissionCode);
        services.AddSingleton<IUserRoleRepository>(repository);
        services.AddSingleton<IRoleRepository>(repository);
        services.AddSingleton<IPermissionRepository>(repository);
        services.AddSingleton<ISportRepository>(repository);
        return services.BuildServiceProvider();
    }

    private sealed class StubRepository(string permissionCode) : IUserRoleRepository, IRoleRepository, IPermissionRepository, ISportRepository
    {
        public Task<UserRoleAssignment?> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<UserRoleAssignment?>(new(userId, ["CUSTOM_ROLE"], [permissionCode]));
        public Task ReplaceUserRolesAsync(UserRoleAssignment assignment, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Role?> GetByIdWithPermissionsAsync(Guid roleId, CancellationToken cancellationToken) => Task.FromResult<Role?>(null);
        public Task<Role?> GetForUpdateAsync(Guid roleId, CancellationToken cancellationToken) => Task.FromResult<Role?>(null);
        public Task<PaginatedResult<Role>> ListAsync(GetRolesQuery query, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Role>> GetByIdsAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> ExistsByNormalizedNameAsync(string normalizedName, Guid? excludedRoleId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public void Add(Role role) => throw new NotSupportedException();
        Task<PaginatedResult<Permission>> IPermissionRepository.ListAsync(BaseQuery query, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<IReadOnlyCollection<Permission>> GetPermissionsAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Permission>> GetByCodesAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Permission?> GetByIdAsync(Guid permissionId, CancellationToken cancellationToken) => Task.FromResult<Permission?>(null);
        public Task<bool> HasActiveDuplicateAsync(string code, string name, Guid? excludedSportId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public void Add(Sport sport) => throw new NotSupportedException();
        public void Update(Sport sport) => throw new NotSupportedException();
        Task<Sport?> ISportRepository.GetForUpdateAsync(Guid sportId, CancellationToken cancellationToken) => throw new NotSupportedException();
        Task<Sport?> ISportRepository.GetByIdAsync(Guid sportId, CancellationToken cancellationToken) => Task.FromResult<Sport?>(null);
        public Task<PaginatedResult<Sport>> ListAsync(GetSportsQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
