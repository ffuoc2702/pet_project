using MatchingSports.Domain.Entities;
using MatchingSports.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MatchingSports.Tests;

public sealed class IdentityModelTests
{
    [Fact]
    public void IdentityModelUsesEmailAndDoesNotPersistPhoneFields()
    {
        using var db = CreateDbContext();
        var user = db.Model.FindEntityType(typeof(User));

        Assert.NotNull(user);
        Assert.Equal("users", user.GetTableName());
        Assert.NotNull(user.FindProperty(nameof(User.Email)));
        Assert.NotNull(user.FindProperty(nameof(User.NormalizedEmail)));
        Assert.Null(user.FindProperty(nameof(User.PhoneNumber)));
        Assert.Null(user.FindProperty(nameof(User.PhoneNumberConfirmed)));
    }

    [Fact]
    public void IdentityStoresUseApplicationOwnedTables()
    {
        using var db = CreateDbContext();

        Assert.Equal("roles", db.Model.FindEntityType(typeof(Role))?.GetTableName());
        Assert.Equal("user_roles", db.Model.FindEntityType(typeof(IdentityUserRole<Guid>))?.GetTableName());
        Assert.Equal("user_logins", db.Model.FindEntityType(typeof(IdentityUserLogin<Guid>))?.GetTableName());
        Assert.Equal("user_tokens", db.Model.FindEntityType(typeof(IdentityUserToken<Guid>))?.GetTableName());
    }

    [Fact]
    public void RefreshSessionsUseOptimisticConcurrency()
    {
        using var db = CreateDbContext();
        var session = db.Model.FindEntityType(typeof(AuthSession));

        Assert.True(session?.FindProperty(nameof(AuthSession.ConcurrencyToken))?.IsConcurrencyToken);
    }

    [Fact]
    public void PermissionsHaveUniqueCodesAndRoleLinksUseCompositeKeys()
    {
        using var db = CreateDbContext();
        var permission = db.Model.FindEntityType(typeof(Permission))!;
        var link = db.Model.FindEntityType(typeof(RolePermission))!;

        Assert.Equal("permissions", permission.GetTableName());
        Assert.Contains(permission.GetIndexes(), index => index.IsUnique &&
            index.Properties.Single().Name == nameof(Permission.Code));
        Assert.Equal("role_permissions", link.GetTableName());
        Assert.Equal([nameof(RolePermission.RoleId), nameof(RolePermission.PermissionId)],
            link.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Equal(2, link.GetForeignKeys().Count());
        Assert.Null(db.Model.FindEntityType(typeof(IdentityRole<Guid>)));
    }

    private static MatchingSportsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MatchingSportsDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=model_validation;Username=postgres;Password=postgres",
                npgsql => npgsql.UseNetTopologySuite())
            .Options;
        return new MatchingSportsDbContext(options);
    }
}
