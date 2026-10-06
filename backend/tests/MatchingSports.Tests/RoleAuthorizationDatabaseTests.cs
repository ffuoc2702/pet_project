using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using MatchingSports.Api.Contracts;
using MatchingSports.Api.Controllers;
using MatchingSports.Api.ErrorHandling;
using MatchingSports.Api.Identity;
using MatchingSports.Application;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Permission;
using MatchingSports.Application.Features.Role;
using MatchingSports.Application.Features.Sport;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Infrastructure;
using MatchingSports.Infrastructure.Persistence;
using MatchingSports.Infrastructure.Persistence.Seed;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace MatchingSports.Tests;

public sealed class RoleAuthorizationDatabaseTests
{
    [RbacDatabaseFact]
    public async Task DynamicRolesConcurrencyRegistrationAndRepeatableSeedWorkOnPostgres()
    {
        var connection = Environment.GetEnvironmentVariable("MATCHINGSPORTS_RBAC_TEST_CONNECTION")!;
        var databaseName = new NpgsqlConnectionStringBuilder(connection).Database;
        // Never execute this fixture against a development or production database.
        Assert.StartsWith("matchingsports_rbac_test_", databaseName, StringComparison.Ordinal);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:matchingsports"] = connection,
            ["Auth:Jwt:SigningKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)),
            ["Auth:Otp:Pepper"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)),
            ["Auth:BootstrapAdminEmail"] = null,
            ["Seed:Admin:Enabled"] = "true",
            ["Seed:Admin:Email"] = "  RBAC-MANAGER@example.test  ",
            ["Seed:Admin:Password"] = "Testing-Password123!",
            ["Seed:Admin:DisplayName"] = "  RBAC manager  "
        }).Build();
        await VerifyInvalidAdminPasswordRollsBackAsync(configuration);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddSingleton<IOtpEmailSender, NoDeliveryEmailSender>();
        await using var provider = services.BuildServiceProvider();
        await Task.WhenAll(provider.InitializeDatabaseAsync(true), provider.InitializeDatabaseAsync(true));

        Guid actorId;
        Guid userRoleId;
        Guid adminRoleId;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
            var seededAdmin = Assert.Single(await db.Users.Include(user => user.Profile).ToListAsync());
            Assert.Equal("rbac-manager@example.test", seededAdmin.Email);
            Assert.True(seededAdmin.EmailConfirmed);
            Assert.Equal("RBAC manager", seededAdmin.Profile!.DisplayName);
            Assert.Equal(InitialPermissionCatalog.All.Count, await db.Permissions.CountAsync());
            Assert.Equal(2, await db.Roles.CountAsync());
            userRoleId = (await db.Roles.SingleAsync(role => role.SeedKey == RoleNames.User)).Id;
            adminRoleId = (await db.Roles.SingleAsync(role => role.SeedKey == RoleNames.Admin)).Id;
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            Assert.True(await manager.CheckPasswordAsync(seededAdmin, "Testing-Password123!"));
            Assert.Equal([RoleNames.Admin], await manager.GetRolesAsync(seededAdmin));
            Assert.True((await db.Roles.SingleAsync(role => role.Id == adminRoleId)).BootstrapCompleted);
            actorId = seededAdmin.Id;
        }

        await VerifyListFiltersAsync(provider, actorId);
        await VerifySportCrudAsync(provider, actorId);

        RoleDto customRole;
        await using (var scope = provider.CreateAsyncScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            customRole = await mediator.Send(new CreateRoleCommand(actorId, "moderator", null, true));
            customRole = await mediator.Send(new UpdateRolePermissionsCommand(actorId, customRole.Id,
                [PermissionCodes.ListSports, PermissionCodes.ReadSports, PermissionCodes.ReadOwnProfile], customRole.ConcurrencyStamp));
            var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
            var userRole = await db.Roles.SingleAsync(role => role.Id == userRoleId);
            await mediator.Send(new UpdateRoleCommand(actorId, userRole.Id, "MEMBER", null, true, false, userRole.ConcurrencyStamp!));
        }

        Guid registeredId;
        string memberAccessToken;
        await using (var scope = provider.CreateAsyncScope())
        {
            var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();
            var registered = await identity.RegisterAsync("rbac-member@example.test", "Testing-Password123!", "Member", null, CancellationToken.None);
            registeredId = registered.UserId;
            var repository = scope.ServiceProvider.GetRequiredService<IUserRoleRepository>();
            var authorization = await repository.GetUserRolesAsync(registeredId, CancellationToken.None);
            Assert.Equal([customRole.Id], authorization!.RoleIds);
            Assert.Equal(["MODERATOR"], authorization.Roles);
            var google = await identity.ExternalLoginAsync("Google", "rbac-google-test-provider-key", "rbac-google@example.test", "Google member", null, null, CancellationToken.None);
            Assert.Equal(["MODERATOR"], google.User.Roles);
            memberAccessToken = google.AccessToken;
        }

        customRole = await VerifyHttpContractsAsync(configuration, memberAccessToken, customRole);

        await using (var scope = provider.CreateAsyncScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var assignment = await mediator.Send(new GetUserRolesQuery(actorId, registeredId));
            var updated = await mediator.Send(new ReplaceUserRolesCommand(actorId, registeredId,
                [customRole.Id, userRoleId, customRole.Id], assignment.ConcurrencyStamp!));
            Assert.Equal(2, updated.RoleIds.Count);
            var authorization = await scope.ServiceProvider.GetRequiredService<IUserRoleRepository>().GetUserRolesAsync(registeredId, CancellationToken.None);
            Assert.Equal(1, authorization!.Permissions.Count(code => code == PermissionCodes.ReadSports));
            await Assert.ThrowsAsync<ConflictException>(() => mediator.Send(new ReplaceUserRolesCommand(actorId, registeredId, [], assignment.ConcurrencyStamp!)));
        }

        // Two contexts read the same version. The second transaction must roll back its grants AND audit.
        await using (var first = provider.CreateAsyncScope())
        await using (var second = provider.CreateAsyncScope())
        {
            var firstDb = first.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
            var secondDb = second.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
            var firstRole = await firstDb.Roles.Include(role => role.Permissions).ThenInclude(link => link.Permission).SingleAsync(role => role.Id == customRole.Id);
            var secondRole = await secondDb.Roles.Include(role => role.Permissions).ThenInclude(link => link.Permission).SingleAsync(role => role.Id == customRole.Id);
            var stamp = firstRole.ConcurrencyStamp!;
            Assert.Equal(stamp, secondRole.ConcurrencyStamp);
            var auditCount = await firstDb.Set<AuthorizationAuditEntry>().CountAsync();
            customRole = await first.ServiceProvider.GetRequiredService<IMediator>().Send(new UpdateRolePermissionsCommand(actorId,
                customRole.Id, [PermissionCodes.ReadSports], stamp));
            var error = await Assert.ThrowsAsync<ConflictException>(() => second.ServiceProvider.GetRequiredService<IMediator>()
                .Send(new UpdateRolePermissionsCommand(actorId, customRole.Id, [PermissionCodes.ReadOwnProfile], stamp)));
            Assert.Equal(ErrorCodes.ConcurrencyConflict, error.Code);
            await using var check = provider.CreateAsyncScope();
            var checkDb = check.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
            Assert.Equal(auditCount + 1, await checkDb.Set<AuthorizationAuditEntry>().CountAsync());
            var role = await checkDb.Roles.Include(item => item.Permissions).ThenInclude(link => link.Permission).SingleAsync(item => item.Id == customRole.Id);
            Assert.Equal(PermissionCodes.ReadSports, Assert.Single(role.Permissions).Permission.Code);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var assignment = await mediator.Send(new GetUserRolesQuery(actorId, registeredId));
            await mediator.Send(new ReplaceUserRolesCommand(actorId, registeredId, [customRole.Id], assignment.ConcurrencyStamp!));
            await mediator.Send(new UpdateRoleCommand(actorId, customRole.Id, "MODERATOR", null, false, false, customRole.ConcurrencyStamp));
            var repository = scope.ServiceProvider.GetRequiredService<IUserRoleRepository>();
            var authorization = await repository.GetUserRolesAsync(registeredId, CancellationToken.None);
            Assert.Empty(authorization!.Permissions);
            Assert.Empty(authorization.Roles);
            assignment = await mediator.Send(new GetUserRolesQuery(actorId, registeredId));
            await mediator.Send(new ReplaceUserRolesCommand(actorId, registeredId, [], assignment.ConcurrencyStamp!));
            var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
            var seededUserRole = await db.Roles.Include(role => role.Permissions).ThenInclude(link => link.Permission).SingleAsync(role => role.Id == userRoleId);
            seededUserRole.ReplacePermissions([]);
            var seededAdminRole = await db.Roles.SingleAsync(role => role.Id == adminRoleId);
            seededAdminRole.UpdateDetails("ACCESS_MANAGER", null, true, false);
            await db.SaveChangesAsync();
        }

        await Task.WhenAll(provider.InitializeDatabaseAsync(true), provider.InitializeDatabaseAsync(true));
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
            Assert.Equal(4, await db.Roles.CountAsync());
            Assert.Equal(InitialPermissionCatalog.All.Count, await db.Permissions.CountAsync());
            Assert.False(await db.Roles.AnyAsync(role => role.Name == RoleNames.User || role.Name == RoleNames.Admin));
            var seededUserRole = await db.Roles.Include(role => role.Permissions).SingleAsync(role => role.SeedKey == RoleNames.User);
            Assert.False(seededUserRole.IsDefaultForRegistration);
            Assert.Empty(seededUserRole.Permissions);
            Assert.False((await db.Roles.SingleAsync(role => role.Id == customRole.Id)).IsActive);
            Assert.False(await db.Set<IdentityUserRole<Guid>>().AnyAsync(item => item.UserId == registeredId));
        }

        await VerifyListPermissionUpgradeAsync(provider);
        await VerifyAdminSeedDoesNotResetOrRepromoteAsync(provider, actorId, adminRoleId);
    }

    private static async Task VerifySportCrudAsync(IServiceProvider provider, Guid actorId)
    {
        var code = $"CRUD_{Guid.NewGuid():N}"[..30];
        Guid sportId;
        await using (var scope = provider.CreateAsyncScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var created = await mediator.Send(new CreateSportCommand(actorId, $"  {code.ToLowerInvariant()}  ", "  Test sport  ",
                [new SportFormatInput(null, "SINGLES", "Singles", 1, "NEW"), new SportFormatInput(null, "OLD", "Old format", 2, "NEW")]));
            sportId = created.Id;
            Assert.Equal(code, created.Code);
            Assert.True(created.IsActive);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
            var repository = scope.ServiceProvider.GetRequiredService<ISportRepository>();
            var read = await repository.GetByIdAsync(sportId, CancellationToken.None);
            Assert.NotNull(read);
            Assert.DoesNotContain(db.ChangeTracker.Entries<Sport>(), entry => entry.Entity.Id == sportId);
            var tracked = await repository.GetForUpdateAsync(sportId, CancellationToken.None);
            Assert.NotNull(tracked);
            Assert.Equal(EntityState.Unchanged, db.Entry(tracked).State);
            var updated = await mediator.Send(new UpdateSportCommand(actorId, sportId, code, "  Updated sport  ",
                [new SportFormatInput(tracked.Formats.Single(format => format.Code == "SINGLES").Id, "DOUBLES", "Doubles", 2, "MODIFY"),
                 new SportFormatInput(null, "TEAM", "Team", 5, "NEW"),
                 new SportFormatInput(tracked.Formats.Single(format => format.Code == "OLD").Id, null, null, 0, "DELETE")]));
            Assert.Equal("Updated sport", updated.Name);
            Assert.Equal(3, updated.Formats.Count);
            Assert.False(updated.Formats.Single(format => format.Code == "OLD").IsActive);
            Assert.True(updated.Formats.Single(format => format.Code == "TEAM").IsActive);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var query = new GetSportsQuery { ActorUserId = actorId, SearchString = code.ToLowerInvariant(), PageSize = 1 };
            var page = await mediator.Send(query);
            Assert.Equal(sportId, Assert.Single(page.Items).Id);
            Assert.Equal(1, page.TotalItems);
            Assert.Equal(1, page.TotalPages);
            var empty = await mediator.Send(query with { PageNumber = 2 });
            Assert.Empty(empty.Items);
            Assert.Equal(2, empty.PageNumber);
            Assert.Equal(1, empty.TotalItems);
            var deactivated = await mediator.Send(new DeactivateSportCommand(actorId, sportId));
            Assert.False(deactivated.IsActive);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var detail = await mediator.Send(new GetSportByIdQuery(actorId, sportId));
            Assert.False(detail.IsActive);
            Assert.Equal("Updated sport", detail.Name);
            Assert.Equal(3, detail.Formats.Count);
            Assert.Equal(2, detail.Formats.Single(format => format.Code == "DOUBLES").SideSize);
            Assert.Equal(5, detail.Formats.Single(format => format.Code == "TEAM").SideSize);
            Assert.False(detail.Formats.Single(format => format.Code == "OLD").IsActive);
            var query = new GetSportsQuery { ActorUserId = actorId, SportCode = code };
            var active = await mediator.Send(query);
            Assert.Empty(active.Items);
            Assert.Equal(0, active.TotalItems);
            var all = await mediator.Send(query with { ActiveOnly = false });
            Assert.False(Assert.Single(all.Items).IsActive);
            Assert.Equal(1, all.TotalItems);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var reused = await mediator.Send(new CreateSportCommand(actorId, code.ToLowerInvariant(), "Duplicate",
                [new SportFormatInput(null, "DEFAULT", "Default", 1, "NEW")]));
            Assert.True(reused.IsActive);
            Assert.NotEqual(sportId, reused.Id);
        }
    }

    private static async Task VerifyListFiltersAsync(IServiceProvider provider, Guid actorId)
    {
        await using var scope = provider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
        var roleQuery = new GetRolesQuery { ActorUserId = actorId, SearchString = "  admin  ", PageSize = 1 };
        var roles = await mediator.Send(roleQuery);
        Assert.Equal(RoleNames.Admin, Assert.Single(roles.Items).Name);
        Assert.Equal(1, roles.TotalItems);
        var emptyRoles = await mediator.Send(roleQuery with { PageNumber = 2 });
        Assert.Empty(emptyRoles.Items);
        Assert.Equal(1, emptyRoles.TotalItems);
        var matrix = await mediator.Send(new GetRolePermissionMatrixQuery { ActorUserId = actorId, SearchString = "  admin  ", PageSize = 1 });
        Assert.Equal(RoleNames.Admin, Assert.Single(matrix.Roles.Items).Name);
        Assert.Equal(1, matrix.Roles.TotalItems);
        Assert.Equal(InitialPermissionCatalog.All.Count, matrix.Permissions.Count);

        var permission = await db.Permissions.SingleAsync(item => item.Code == PermissionCodes.ReadSports);
        var permissionQuery = new GetPermissionsQuery
        {
            ActorUserId = actorId,
            SearchString = "  SPORTS.READ  ",
            PageSize = 1,
            FromDate = permission.UpdatedAt,
            ToDate = permission.UpdatedAt
        };
        var permissions = await mediator.Send(permissionQuery);
        Assert.Equal(permission.Id, Assert.Single(permissions.Items).Id);
        Assert.Equal(1, permissions.TotalItems);
        var outsideDate = await mediator.Send(permissionQuery with { FromDate = permission.UpdatedAt.AddDays(1), ToDate = null });
        Assert.Empty(outsideDate.Items);
        Assert.Equal(0, outsideDate.TotalItems);
        var sport = await db.Sports.OrderBy(item => item.Code).FirstAsync();
        var sports = await mediator.Send(new GetSportsQuery
        {
            ActorUserId = actorId,
            SportCode = sport.Code,
            SearchString = $"  {sport.Code.ToLowerInvariant()}  ",
            ActiveOnly = false,
            FromDate = sport.UpdatedAt,
            ToDate = sport.UpdatedAt,
            PageSize = 1
        });
        Assert.Equal(sport.Id, Assert.Single(sports.Items).Id);
        Assert.Equal(1, sports.TotalItems);
    }

    private static async Task VerifyInvalidAdminPasswordRollsBackAsync(IConfiguration configuration)
    {
        var invalid = new ConfigurationBuilder().AddConfiguration(configuration)
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Seed:Admin:Password"] = "weak" }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(invalid);
        services.AddInfrastructure(invalid);
        await using var provider = services.BuildServiceProvider();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.InitializeDatabaseAsync(true));
        Assert.DoesNotContain("weak", error.Message, StringComparison.Ordinal);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
        Assert.Empty(await db.Users.ToListAsync());
        Assert.Empty(await db.UserProfiles.ToListAsync());
        Assert.Empty(await db.Roles.ToListAsync());
        Assert.Empty(await db.Permissions.ToListAsync());
    }

    private static async Task VerifyListPermissionUpgradeAsync(IServiceProvider provider)
    {
        Guid customRoleId;
        var newListCodes = InitialPermissionCatalog.All.Where(definition => definition.LegacyReadCode is not null)
            .Select(definition => definition.Code).ToArray();
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
            // Simulate the pre-upgrade catalog, only in this isolated fixture database.
            await db.Permissions.Where(permission => newListCodes.Contains(permission.Code)).ExecuteDeleteAsync();
            var role = Role.Create("LEGACY_SUPPORT");
            role.GrantPermission(await db.Permissions.SingleAsync(permission => permission.Code == PermissionCodes.ReadSports));
            db.Roles.Add(role);
            await db.SaveChangesAsync();
            customRoleId = role.Id;
        }
        await provider.InitializeDatabaseAsync(true);
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
            var role = await db.Roles.Include(item => item.Permissions).ThenInclude(link => link.Permission).SingleAsync(item => item.Id == customRoleId);
            Assert.Contains(role.Permissions, link => link.Permission.Code == PermissionCodes.ListSports);
            Assert.Contains(role.Permissions, link => link.Permission.Code == PermissionCodes.ReadSports);
            var userRole = await db.Roles.Include(item => item.Permissions).SingleAsync(item => item.SeedKey == RoleNames.User);
            Assert.Empty(userRole.Permissions); // The old read grants were revoked before upgrade.
            role.RevokePermission(role.Permissions.Single(link => link.Permission.Code == PermissionCodes.ListSports).PermissionId);
            await db.SaveChangesAsync();
        }
        await provider.InitializeDatabaseAsync(true);
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
            var role = await db.Roles.Include(item => item.Permissions).ThenInclude(link => link.Permission).SingleAsync(item => item.Id == customRoleId);
            Assert.DoesNotContain(role.Permissions, link => link.Permission.Code == PermissionCodes.ListSports);
            Assert.Contains(role.Permissions, link => link.Permission.Code == PermissionCodes.ReadSports);
        }
    }

    private static async Task VerifyAdminSeedDoesNotResetOrRepromoteAsync(IServiceProvider provider, Guid adminId, Guid adminRoleId)
    {
        await using (var scope = provider.CreateAsyncScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var admin = (await manager.FindByIdAsync(adminId.ToString()))!;
            Assert.True((await manager.ChangePasswordAsync(admin, "Testing-Password123!", "Changed-Password456!")).Succeeded);
            var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
            db.RemoveRange(await db.Set<IdentityUserRole<Guid>>().Where(item => item.UserId == adminId && item.RoleId == adminRoleId).ToListAsync());
            await db.SaveChangesAsync();
        }
        await provider.InitializeDatabaseAsync(true);
        await using (var scope = provider.CreateAsyncScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var admin = (await manager.FindByIdAsync(adminId.ToString()))!;
            Assert.True(await manager.CheckPasswordAsync(admin, "Changed-Password456!"));
            Assert.False(await manager.CheckPasswordAsync(admin, "Testing-Password123!"));
            var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
            Assert.False(await db.Set<IdentityUserRole<Guid>>().AnyAsync(item => item.UserId == adminId && item.RoleId == adminRoleId));
            Assert.Equal(1, await db.Users.CountAsync(user => user.Id == adminId));
        }
    }

    private static async Task<RoleDto> VerifyHttpContractsAsync(IConfiguration configuration, string memberAccessToken, RoleDto role)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddConfiguration(configuration);
        builder.Logging.ClearProviders();
        builder.Services.AddApplication(null, typeof(ContractMappingProfile).Assembly);
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddCurrentUser();
        builder.Services.AddControllers().AddApplicationPart(typeof(RoleController).Assembly);
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        await using var app = builder.Build();
        app.Urls.Add("http://127.0.0.1:0");
        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/role/matrix")).StatusCode);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberAccessToken);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/sport")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/role", new { name = "NOT_ALLOWED" })).StatusCode);

            await using var scope = app.Services.CreateAsyncScope();
            var tokens = await scope.ServiceProvider.GetRequiredService<IIdentityService>()
                .LoginAsync("rbac-manager@example.test", "Testing-Password123!", null, null, CancellationToken.None);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
            var create = await client.PostAsJsonAsync("/api/v1/role", new { name = "EVENT_ORGANIZER", isDefaultForRegistration = false });
            Assert.Equal(HttpStatusCode.OK, create.StatusCode);
            var created = (await create.Content.ReadFromJsonAsync<BaseResponse<RoleDto>>())!.Data;
            var uri = $"/api/v1/role/{created.Id}/permissions";
            var missingStamp = await client.PutAsJsonAsync(uri, new { permissionCodes = new[] { PermissionCodes.ReadSports } });
            Assert.Equal(HttpStatusCode.BadRequest, missingStamp.StatusCode);
            var granted = await client.PutAsJsonAsync(uri, new { permissionCodes = new[] { PermissionCodes.ReadSports }, concurrencyStamp = created.ConcurrencyStamp });
            Assert.Equal(HttpStatusCode.OK, granted.StatusCode);
            var updated = (await granted.Content.ReadFromJsonAsync<BaseResponse<RoleDto>>())!.Data;
            var stale = await client.PutAsJsonAsync(uri, new { permissionCodes = Array.Empty<string>(), concurrencyStamp = created.ConcurrencyStamp });
            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
            using (var problem = JsonDocument.Parse(await stale.Content.ReadAsStringAsync()))
                Assert.Equal(ErrorCodes.ConcurrencyConflict, problem.RootElement.GetProperty("code").GetString());
            var rename = await client.PutAsJsonAsync($"/api/v1/role/{created.Id}", new
            {
                name = "EVENT_COORDINATOR",
                isActive = true,
                isDefaultForRegistration = false,
                concurrencyStamp = updated.ConcurrencyStamp
            });
            Assert.Equal(HttpStatusCode.OK, rename.StatusCode);
            var matrixResponse = await client.GetAsync("/api/v1/role/matrix");
            Assert.Equal(HttpStatusCode.OK, matrixResponse.StatusCode);
            var matrix = (await matrixResponse.Content.ReadFromJsonAsync<BaseResponse<RolePermissionMatrixDto>>())!.Data;
            Assert.Contains(matrix.Roles.Items, row => row.Name == "EVENT_COORDINATOR");
            Assert.Equal(InitialPermissionCatalog.All.Count, matrix.Permissions.Count);

            var revoke = await client.PutAsJsonAsync($"/api/v1/role/{role.Id}/permissions", new
            {
                permissionCodes = new[] { PermissionCodes.ReadOwnProfile },
                concurrencyStamp = role.ConcurrencyStamp
            });
            Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);
            role = (await revoke.Content.ReadFromJsonAsync<BaseResponse<RoleDto>>())!.Data;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberAccessToken);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/sport")).StatusCode);
            var footballPath = "/api/v1/sport/00000000-0000-0000-0000-000000000001";
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(footballPath)).StatusCode);

            // The same signed token follows current database grants; list and read never imply one another.
            foreach (var code in new[] { PermissionCodes.ReadSports, PermissionCodes.ListSports })
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
                var change = await client.PutAsJsonAsync($"/api/v1/role/{role.Id}/permissions", new
                {
                    permissionCodes = new[] { PermissionCodes.ReadOwnProfile, code },
                    concurrencyStamp = role.ConcurrencyStamp
                });
                Assert.Equal(HttpStatusCode.OK, change.StatusCode);
                role = (await change.Content.ReadFromJsonAsync<BaseResponse<RoleDto>>())!.Data;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberAccessToken);
                Assert.Equal(code == PermissionCodes.ListSports ? HttpStatusCode.OK : HttpStatusCode.Forbidden,
                    (await client.GetAsync("/api/v1/sport")).StatusCode);
                Assert.Equal(code == PermissionCodes.ReadSports ? HttpStatusCode.OK : HttpStatusCode.Forbidden,
                    (await client.GetAsync(footballPath)).StatusCode);
            }

            foreach (var canList in new[] { true, false })
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
                var change = await client.PutAsJsonAsync($"/api/v1/role/{role.Id}/permissions", new
                {
                    permissionCodes = canList ? new[] { PermissionCodes.ListRoles, PermissionCodes.ListPermissions }
                        : [PermissionCodes.ReadRoles, PermissionCodes.ReadPermissions],
                    concurrencyStamp = role.ConcurrencyStamp
                });
                Assert.Equal(HttpStatusCode.OK, change.StatusCode);
                role = (await change.Content.ReadFromJsonAsync<BaseResponse<RoleDto>>())!.Data;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberAccessToken);
                foreach (var path in new[] { "/api/v1/role", "/api/v1/permission", "/api/v1/role/matrix" })
                    Assert.Equal(canList ? HttpStatusCode.OK : HttpStatusCode.Forbidden, (await client.GetAsync(path)).StatusCode);
                foreach (var path in new[] { $"/api/v1/role/{created.Id}", $"/api/v1/permission/{matrix.Permissions.First().Id}" })
                    Assert.Equal(canList ? HttpStatusCode.Forbidden : HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
            var restore = await client.PutAsJsonAsync($"/api/v1/role/{role.Id}/permissions", new
            {
                permissionCodes = new[] { PermissionCodes.ReadOwnProfile, PermissionCodes.ListSports, PermissionCodes.ReadSports },
                concurrencyStamp = role.ConcurrencyStamp
            });
            Assert.Equal(HttpStatusCode.OK, restore.StatusCode);
            return (await restore.Content.ReadFromJsonAsync<BaseResponse<RoleDto>>())!.Data;
        }
        finally
        {
            await app.StopAsync();
        }
    }

    private sealed class NoDeliveryEmailSender : IOtpEmailSender
    {
        public Task SendAsync(string recipientEmail, string otp, OtpPurpose purpose, TimeSpan validFor, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

public sealed class RbacDatabaseFactAttribute : FactAttribute
{
    public RbacDatabaseFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MATCHINGSPORTS_RBAC_TEST_CONNECTION")))
            Skip = "Set MATCHINGSPORTS_RBAC_TEST_CONNECTION to an empty isolated matchingsports_rbac_test_* PostgreSQL database.";
    }
}
