using System.Text.Json;
using MatchingSports.Api.Contracts;
using MatchingSports.Application;
using MatchingSports.Application.Features.Role;
using MatchingSports.Application.Features.User;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Infrastructure;
using MatchingSports.Infrastructure.Persistence;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace MatchingSports.Tests;

public sealed class UserDirectoryPersistenceTests
{
    [RbacDatabaseFact]
    public async Task UserDirectoryAndRoleMatrixApplyFiltersBeforePagingWithoutExposingIdentitySecrets()
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("MATCHINGSPORTS_RBAC_TEST_CONNECTION")!);
        Assert.StartsWith("matchingsports_rbac_test_", connection.Database, StringComparison.Ordinal);
        connection.Database = $"matchingsports_rbac_test_directory_{Guid.NewGuid():N}";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:matchingsports"] = connection.ConnectionString,
            ["Auth:Jwt:SigningKey"] = new string('j', 48),
            ["Auth:Otp:Pepper"] = new string('o', 48)
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication(null, typeof(ContractMappingProfile).Assembly);
        services.AddInfrastructure(configuration);
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
        try
        {
            await db.Database.MigrateAsync();
            var users = Enumerable.Range(0, 5).Select(index => User.CreateVerifiedAccount($"directory-{index}@example.test", $"Player {index}")).ToArray();
            foreach (var user in users)
            {
                user.NormalizedEmail = user.Email!.ToUpperInvariant();
                user.NormalizedUserName = user.UserName!.ToUpperInvariant();
            }
            users[3].Status = UserStatus.SUSPENDED;
            users[4].Status = UserStatus.DELETED;
            users[4].DeletedAt = DateTimeOffset.UtcNow;
            users[0].PasswordHash = "private-password-hash";
            users[0].SecurityStamp = "private-security-stamp";
            var active = Role.Create("STAFF", "Active role");
            var inactive = Role.Create("INACTIVE", "Inactive role");
            inactive.UpdateDetails("INACTIVE", "Inactive role", false, false);
            var permissions = new[] { PermissionCodes.ListUsers, PermissionCodes.ListRoles, PermissionCodes.ListPermissions }
                .Select(code => Permission.Create(code, code)).ToArray();
            active.ReplacePermissions(permissions);
            db.Users.AddRange(users);
            db.Roles.AddRange(active, inactive);
            await db.SaveChangesAsync();
            db.Add(new IdentityUserRole<Guid> { UserId = users[0].Id, RoleId = active.Id });
            db.Add(new IdentityUserRole<Guid> { UserId = users[1].Id, RoleId = inactive.Id });
            await db.SaveChangesAsync();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var query = new GetUsersQuery { ActorUserId = users[0].Id, PageSize = 2 };
            var first = await mediator.Send(query);
            Assert.Equal(4, first.TotalItems);
            Assert.Equal(users.Take(2).Select(user => user.Id), first.Items.Select(user => user.Id));
            Assert.True(first.HasNextPage);
            Assert.Equal("STAFF", Assert.Single(first.Items[0].Roles).Name);
            Assert.False(Assert.Single(first.Items[1].Roles).IsActive);
            var serialized = JsonSerializer.Serialize(first.Items);
            Assert.DoesNotContain("private-password-hash", serialized, StringComparison.Ordinal);
            Assert.DoesNotContain("private-security-stamp", serialized, StringComparison.Ordinal);
            var second = await mediator.Send(query with { PageNumber = 2 });
            Assert.Equal(users.Skip(2).Take(2).Select(user => user.Id), second.Items.Select(user => user.Id));
            var empty = await mediator.Send(query with { PageNumber = 3 });
            Assert.Empty(empty.Items);
            Assert.Equal(4, empty.TotalItems);
            Assert.Single((await mediator.Send(query with { SearchString = "DIRECTORY-1" })).Items);
            Assert.Equal(users[2].Id, Assert.Single((await mediator.Send(query with { SearchString = "PLAYER 2" })).Items).Id);
            Assert.Equal(4, (await mediator.Send(query with { SearchString = "   " })).TotalItems);
            Assert.Equal(1, (await mediator.Send(query with { Status = UserStatus.SUSPENDED })).TotalItems);
            Assert.Equal(3, (await mediator.Send(query with { Status = UserStatus.ACTIVE })).TotalItems);
            Assert.Empty((await mediator.Send(query with { FromDate = DateTimeOffset.UtcNow.AddDays(1) })).Items);
            var roleQuery = new GetRolesQuery { ActorUserId = users[0].Id, IsActive = false, PageSize = 1 };
            Assert.Equal(inactive.Id, Assert.Single((await mediator.Send(roleQuery)).Items).Id);
            var matrix = await mediator.Send(new GetRolePermissionMatrixQuery { ActorUserId = users[0].Id, IsActive = false, PageSize = 1 });
            Assert.Equal(1, matrix.Roles.TotalItems);
            Assert.Equal(inactive.Id, Assert.Single(matrix.Roles.Items).Id);
            Assert.Equal(3, matrix.Permissions.Count);
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
