using System.Text.Json;
using AutoMapper;
using MatchingSports.Api.Contracts;
using MatchingSports.Api.Controllers;
using MatchingSports.Application;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Permission;
using MatchingSports.Application.Features.Role;
using MatchingSports.Domain.Entities;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace MatchingSports.Tests;

public sealed class RoleManagementTests
{
    [Fact]
    public void CustomRoleNormalizesDetailsAndCanBeRegistrationDefault()
    {
        var role = Role.Create("  event-organizer  ", "  Quản lý sự kiện  ", true);
        Assert.Equal("EVENT-ORGANIZER", role.Name);
        Assert.Equal(role.Name, role.NormalizedName);
        Assert.Equal("Quản lý sự kiện", role.Description);
        Assert.True(role.IsDefaultForRegistration);
        Assert.Empty(role.Permissions);
    }

    [Fact]
    public void InvalidUpdateDoesNotPartiallyMutateRole()
    {
        var role = Role.Create("MODERATOR", "Existing", true);
        var stamp = role.ConcurrencyStamp;
        Assert.Throws<ArgumentException>(() => role.UpdateDetails("SUPPORT", "New", false, true));
        Assert.Equal("MODERATOR", role.Name);
        Assert.Equal("Existing", role.Description);
        Assert.True(role.IsActive);
        Assert.True(role.IsDefaultForRegistration);
        Assert.Equal(stamp, role.ConcurrencyStamp);
    }

    [Fact]
    public void SeedIdentitySurvivesRenameAndDoesNotRestoreRegistrationDefault()
    {
        var role = Role.Create(RoleNames.User, isDefaultForRegistration: true);
        role.InitializeSeedIdentity(RoleNames.User);
        role.UpdateDetails("MEMBER", null, false, false);
        role.InitializeRegistrationDefault(true);
        Assert.Equal(RoleNames.User, role.SeedKey);
        Assert.Equal("MEMBER", role.Name);
        Assert.False(role.IsDefaultForRegistration);
        Assert.False(role.IsActive);
    }

    [Theory]
    [InlineData("")]
    [InlineData("two words")]
    [InlineData("role.*")]
    public void InvalidRoleNamesAreRejected(string name) =>
        Assert.ThrowsAny<ArgumentException>(() => Role.Create(name));

    [Fact]
    public async Task CreateRoleRequiresPermissionNotSpecificRoleAndAuditsOnce()
    {
        var repository = new StubRepository(PermissionCodes.CreateRoles);
        using var provider = CreateProvider(repository);
        var mediator = provider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new CreateRoleCommand(repository.Actor.UserId, "moderator", "Moderates listings", false), CancellationToken.None);
        Assert.Equal("MODERATOR", result.Name);
        Assert.Empty(result.Permissions);
        Assert.Equal(1, repository.SaveCount);
        Assert.Single(repository.Roles);
        var audit = Assert.Single(repository.AuditEntries);
        Assert.Equal(PermissionCodes.CreateRoles, audit.Action);
        Assert.Equal(repository.Actor.UserId, audit.ActorUserId);
        Assert.Equal(result.Id, audit.TargetId);
    }

    [Fact]
    public async Task RoleNameConflictDoesNotWriteOrAudit()
    {
        var repository = new StubRepository(PermissionCodes.CreateRoles);
        repository.Roles.Add(Role.Create("MODERATOR"));
        using var provider = CreateProvider(repository);
        var mediator = provider.GetRequiredService<IMediator>();
        var error = await Assert.ThrowsAsync<ConflictException>(() => mediator.Send(
            new CreateRoleCommand(repository.Actor.UserId, "moderator", null, false), CancellationToken.None));
        Assert.Equal(ErrorCodes.DuplicateResource, error.Code);
        Assert.Equal(0, repository.SaveCount);
        Assert.Empty(repository.AuditEntries);
    }

    [Fact]
    public async Task UpdateRoleCanDeactivateCustomRoleAndAuditsBeforeAndAfter()
    {
        var repository = new StubRepository(PermissionCodes.UpdateRoles);
        var role = Role.Create("MODERATOR", isDefaultForRegistration: true);
        repository.Roles.Add(role);
        using var provider = CreateProvider(repository);
        var mediator = provider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new UpdateRoleCommand(repository.Actor.UserId, role.Id, "support", "New description",
            false, false, role.ConcurrencyStamp!), CancellationToken.None);
        Assert.Equal("SUPPORT", result.Name);
        Assert.False(result.IsActive);
        Assert.False(result.IsDefaultForRegistration);
        Assert.Equal(1, repository.SaveCount);
        var audit = Assert.Single(repository.AuditEntries);
        Assert.Contains("MODERATOR", audit.BeforeJson, StringComparison.Ordinal);
        Assert.Contains("SUPPORT", audit.AfterJson, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("stale", typeof(ConflictException))]
    [InlineData("", typeof(RequestValidationException))]
    public async Task UpdateRejectsInvalidVersionBeforeMutation(string stamp, Type exceptionType)
    {
        var repository = new StubRepository(PermissionCodes.UpdateRoles);
        var role = Role.Create("MODERATOR");
        repository.Roles.Add(role);
        using var provider = CreateProvider(repository);
        var mediator = provider.GetRequiredService<IMediator>();
        var error = await Record.ExceptionAsync(() => mediator.Send(new UpdateRoleCommand(repository.Actor.UserId, role.Id,
            "support", null, true, false, stamp), CancellationToken.None));
        Assert.IsType(exceptionType, error);
        Assert.Equal("MODERATOR", role.Name);
        Assert.Equal(0, repository.SaveCount);
        Assert.Empty(repository.AuditEntries);
    }

    [Fact]
    public async Task MatrixIncludesUnassignedPermissionColumnsAndArbitraryRoles()
    {
        var repository = new StubRepository(PermissionCodes.ListRoles, PermissionCodes.ListPermissions);
        var role = Role.Create("CUSTOM_ROLE");
        var granted = Permission.Create("custom.read", "Custom read");
        var unassigned = Permission.Create("custom.write", "Custom write");
        role.GrantPermission(granted);
        repository.Roles.Add(role);
        repository.Permissions.AddRange([granted, unassigned]);
        using var provider = CreateProvider(repository);
        var mediator = provider.GetRequiredService<IMediator>();
        var matrix = await mediator.Send(new GetRolePermissionMatrixQuery { ActorUserId = repository.Actor.UserId }, CancellationToken.None);
        Assert.Equal(2, matrix.Permissions.Count);
        var row = Assert.Single(matrix.Roles.Items);
        Assert.Equal("CUSTOM_ROLE", row.Name);
        Assert.Equal("custom.read", Assert.Single(row.Permissions).Code);
    }

    [Theory]
    [InlineData(PermissionCodes.ListRoles)]
    [InlineData(PermissionCodes.ListPermissions)]
    public async Task MatrixRequiresBothListPermissions(string onlyPermission)
    {
        var repository = new StubRepository(onlyPermission);
        using var provider = CreateProvider(repository);
        var mediator = provider.GetRequiredService<IMediator>();
        await Assert.ThrowsAsync<ForbiddenException>(() => mediator.Send(new GetRolePermissionMatrixQuery { ActorUserId = repository.Actor.UserId }, CancellationToken.None));
    }

    [Theory]
    [InlineData("roles")]
    [InlineData("permissions")]
    [InlineData("matrix")]
    public async Task ListsPreserveRequestedPageMetadataAndCancellation(string list)
    {
        var repository = new StubRepository(PermissionCodes.ListRoles, PermissionCodes.ListPermissions);
        repository.Roles.AddRange([Role.Create("ALPHA"), Role.Create("BRAVO"), Role.Create("CHARLIE")]);
        repository.Permissions.AddRange([Permission.Create("alpha.read", "Alpha"),
            Permission.Create("bravo.read", "Bravo"), Permission.Create("charlie.read", "Charlie")]);
        using var provider = CreateProvider(repository);
        using var cancellation = new CancellationTokenSource();
        var mediator = provider.GetRequiredService<IMediator>();

        for (var page = 1; page <= 3; page++)
        {
            var result = list switch
            {
                "roles" => (await mediator.Send(new GetRolesQuery { ActorUserId = repository.Actor.UserId, PageNumber = page, PageSize = 2 }, cancellation.Token)).Map(role => role.Name),
                "permissions" => (await mediator.Send(new GetPermissionsQuery { ActorUserId = repository.Actor.UserId, PageNumber = page, PageSize = 2 }, cancellation.Token)).Map(permission => permission.Code),
                _ => (await mediator.Send(new GetRolePermissionMatrixQuery { ActorUserId = repository.Actor.UserId, PageNumber = page, PageSize = 2 }, cancellation.Token)).Roles.Map(role => role.Name)
            };
            Assert.Equal(page == 1 ? 2 : page == 2 ? 1 : 0, result.Items.Count);
            Assert.Equal(page, result.PageNumber);
            Assert.Equal(2, result.PageSize);
            Assert.Equal(3, result.TotalItems);
            Assert.Equal(2, result.TotalPages);
            Assert.Equal(page == 1, result.HasNextPage);
            Assert.Equal(page > 1, result.HasPreviousPage);
            Assert.Equal(cancellation.Token, repository.LastListCancellationToken);
        }

        if (list == "matrix")
        {
            var matrix = await mediator.Send(new GetRolePermissionMatrixQuery { ActorUserId = repository.Actor.UserId, PageNumber = 2, PageSize = 2 });
            Assert.Equal(3, matrix.Permissions.Count);
            Assert.Equal("CHARLIE", Assert.Single(matrix.Roles.Items).Name);
        }
        Assert.Equal(0, repository.SaveCount);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    [InlineData(int.MaxValue, 100)]
    public async Task InvalidPaginationStopsAllListHandlers(int pageNumber, int pageSize)
    {
        var repository = new StubRepository(PermissionCodes.ListRoles, PermissionCodes.ListPermissions);
        using var provider = CreateProvider(repository);
        var mediator = provider.GetRequiredService<IMediator>();
        var actor = repository.Actor.UserId;
        var rolesError = await Assert.ThrowsAsync<RequestValidationException>(() => mediator.Send(new GetRolesQuery { ActorUserId = actor, PageNumber = pageNumber, PageSize = pageSize }));
        var permissionsError = await Assert.ThrowsAsync<RequestValidationException>(() => mediator.Send(new GetPermissionsQuery { ActorUserId = actor, PageNumber = pageNumber, PageSize = pageSize }));
        var matrixError = await Assert.ThrowsAsync<RequestValidationException>(() => mediator.Send(new GetRolePermissionMatrixQuery { ActorUserId = actor, PageNumber = pageNumber, PageSize = pageSize }));
        Assert.All(new[] { rolesError, permissionsError, matrixError }, error => Assert.Equal(ErrorCodes.InvalidPagination, error.Code));
        Assert.Equal(0, repository.ListCount);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task ListsUseDefaultPaginationAndRetainTotalsForEmptyData()
    {
        var repository = new StubRepository(PermissionCodes.ListRoles, PermissionCodes.ListPermissions);
        using var provider = CreateProvider(repository);
        var mediator = provider.GetRequiredService<IMediator>();
        var roles = await mediator.Send(new GetRolesQuery { ActorUserId = repository.Actor.UserId });
        var permissions = await mediator.Send(new GetPermissionsQuery { ActorUserId = repository.Actor.UserId });
        var matrix = await mediator.Send(new GetRolePermissionMatrixQuery { ActorUserId = repository.Actor.UserId });
        Assert.All(new[] { roles.Map(role => role.Name), permissions.Map(permission => permission.Code), matrix.Roles.Map(role => role.Name) }, result =>
        {
            Assert.Empty(result.Items);
            Assert.Equal(1, result.PageNumber);
            Assert.Equal(20, result.PageSize);
            Assert.Equal(0, result.TotalItems);
            Assert.Equal(0, result.TotalPages);
            Assert.False(result.HasPreviousPage);
            Assert.False(result.HasNextPage);
        });
    }

    [Fact]
    public async Task ControllersReturnPaginatedContractsForRequestedPage()
    {
        var repository = new StubRepository(PermissionCodes.ListRoles, PermissionCodes.ListPermissions);
        repository.Roles.AddRange([Role.Create("ALPHA"), Role.Create("BRAVO")]);
        repository.Permissions.AddRange([Permission.Create("alpha.read", "Alpha"), Permission.Create("bravo.read", "Bravo")]);
        using var provider = CreateProvider(repository);
        var mediator = provider.GetRequiredService<IMediator>();
        var currentUser = new StubCurrentUser(repository.Actor.UserId);
        var roles = new RoleController(mediator, currentUser, provider.GetRequiredService<IMapper>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var permissions = new PermissionController(mediator, currentUser, provider.GetRequiredService<IMapper>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var roleResult = await roles.GetRoles(new GetRolesRequest { PageNumber = 2, PageSize = 1, SearchString = "role search" }, CancellationToken.None);
        var roleResponse = Assert.IsType<PaginatedResponse<RoleDto>>(Assert.IsType<OkObjectResult>(roleResult.Result).Value);
        Assert.Equal("BRAVO", Assert.Single(roleResponse.Data).Name);
        Assert.Equal("role search", repository.LastListQuery!.SearchString);
        Assert.Equal(currentUser.UserId, ((IPermissionAuthorizedRequest)repository.LastListQuery).ActorUserId);
        Assert.Equal(2, roleResponse.Pagination.TotalItems);
        Assert.Equal(2, roleResponse.Pagination.PageNumber);
        Assert.False(roleResponse.Pagination.HasNextPage);
        Assert.True(roleResponse.Pagination.HasPreviousPage);
        var permissionResult = await permissions.GetPermissions(new GetPermissionsRequest { PageNumber = 2, PageSize = 1, SearchString = "permission search", FromDate = DateTimeOffset.UtcNow.AddDays(-1) }, CancellationToken.None);
        var permissionResponse = Assert.IsType<PaginatedResponse<PermissionDto>>(Assert.IsType<OkObjectResult>(permissionResult.Result).Value);
        Assert.Equal("bravo.read", Assert.Single(permissionResponse.Data).Code);
        Assert.Equal("permission search", repository.LastListQuery!.SearchString);
        Assert.NotNull(repository.LastListQuery.FromDate);
        Assert.Equal(currentUser.UserId, ((IPermissionAuthorizedRequest)repository.LastListQuery).ActorUserId);
        Assert.Equal(2, permissionResponse.Pagination.TotalItems);
        Assert.Equal(2, permissionResponse.Pagination.PageNumber);
        var matrixResult = await roles.GetRolePermissionMatrix(new GetRolePermissionMatrixRequest { PageNumber = 2, PageSize = 1, SearchString = "matrix search" }, CancellationToken.None);
        var matrixResponse = Assert.IsType<BaseResponse<RolePermissionMatrixDto>>(Assert.IsType<OkObjectResult>(matrixResult.Result).Value);
        Assert.Equal("matrix search", repository.LastListQuery!.SearchString);
        Assert.Equal(currentUser.UserId, ((IPermissionAuthorizedRequest)repository.LastListQuery).ActorUserId);
        // Verify the changed matrix contract can be read by JSON clients.
        var matrix = JsonSerializer.Deserialize<BaseResponse<RolePermissionMatrixDto>>(JsonSerializer.Serialize(matrixResponse))!.Data;
        Assert.Equal("BRAVO", Assert.Single(matrix.Roles.Items).Name);
        Assert.Equal(2, matrix.Roles.TotalItems);
        Assert.Equal(2, matrix.Permissions.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RoleQueriesRejectUnsupportedDateFiltersBeforeRepository(bool matrix)
    {
        var repository = new StubRepository(PermissionCodes.ListRoles, PermissionCodes.ListPermissions);
        using var provider = CreateProvider(repository);
        var mediator = provider.GetRequiredService<IMediator>();
        var error = matrix
            ? await Assert.ThrowsAsync<RequestValidationException>(() => mediator.Send(new GetRolePermissionMatrixQuery { ActorUserId = repository.Actor.UserId, FromDate = DateTimeOffset.UtcNow }))
            : await Assert.ThrowsAsync<RequestValidationException>(() => mediator.Send(new GetRolesQuery { ActorUserId = repository.Actor.UserId, ToDate = DateTimeOffset.UtcNow }));
        Assert.Equal(ErrorCodes.InvalidDateRange, error.Code);
        Assert.Equal(0, repository.ListCount);
    }

    private sealed class StubCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId => userId;
    }

    private static ServiceProvider CreateProvider(StubRepository repository)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication(null, typeof(ContractMappingProfile).Assembly);
        services.AddSingleton<IRoleRepository>(repository);
        services.AddSingleton<IPermissionRepository>(repository);
        services.AddSingleton<IUserRoleRepository>(repository);
        services.AddSingleton<IAuthorizationAuditRepository>(repository);
        services.AddSingleton<IUnitOfWork>(repository);
        return services.BuildServiceProvider();
    }

    private sealed class StubRepository(params string[] actorPermissions)
        : IRoleRepository, IUserRoleRepository, IPermissionRepository, IAuthorizationAuditRepository, IUnitOfWork
    {
        public UserRoleAssignment Actor { get; } = new(Guid.NewGuid(), ["ACCESS_MANAGER"], actorPermissions);
        public List<Role> Roles { get; } = [];
        public List<Permission> Permissions { get; } = [];
        public List<AuthorizationAuditEntry> AuditEntries { get; } = [];
        public int SaveCount { get; private set; }
        public int ListCount { get; private set; }
        public CancellationToken LastListCancellationToken { get; private set; }
        public BaseQuery? LastListQuery { get; private set; }
        public Task<UserRoleAssignment?> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult<UserRoleAssignment?>(Actor);
        public Task ReplaceUserRolesAsync(UserRoleAssignment assignment, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Role?> GetByIdWithPermissionsAsync(Guid roleId, CancellationToken cancellationToken) => GetForUpdateAsync(roleId, cancellationToken);
        public Task<Role?> GetForUpdateAsync(Guid roleId, CancellationToken cancellationToken) => Task.FromResult(Roles.SingleOrDefault(role => role.Id == roleId));
        public Task<PaginatedResult<Role>> ListAsync(GetRolesQuery query, CancellationToken cancellationToken)
        {
            LastListQuery = query;
            ListCount++;
            LastListCancellationToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new PaginatedResult<Role>(Roles.OrderBy(role => role.Name).ThenBy(role => role.Id).Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToArray(), query.PageNumber, query.PageSize, Roles.Count));
        }
        public Task<IReadOnlyCollection<Role>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<Role>>(Roles.Where(role => ids.Contains(role.Id)).ToArray());
        public Task<bool> ExistsByNormalizedNameAsync(string name, Guid? excludedRoleId, CancellationToken cancellationToken) => Task.FromResult(Roles.Any(role => role.NormalizedName == name && role.Id != excludedRoleId));
        public Task<Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Permissions.SingleOrDefault(permission => permission.Id == id));
        Task<PaginatedResult<Permission>> IPermissionRepository.ListAsync(BaseQuery query, CancellationToken cancellationToken)
        {
            LastListQuery = query;
            ListCount++;
            LastListCancellationToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new PaginatedResult<Permission>(Permissions.OrderBy(permission => permission.Code).Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToArray(), query.PageNumber, query.PageSize, Permissions.Count));
        }
        public Task<IReadOnlyCollection<Permission>> GetPermissionsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<Permission>>(Permissions);
        public Task<IReadOnlyCollection<Permission>> GetByCodesAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<Permission>>(Permissions.Where(permission => codes.Contains(permission.Code)).ToArray());
        public void Add(Role role) => Roles.Add(role);
        public void Add(AuthorizationAuditEntry entry) => AuditEntries.Add(entry);
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
