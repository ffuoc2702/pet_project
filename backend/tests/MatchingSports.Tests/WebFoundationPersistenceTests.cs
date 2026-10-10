using MatchingSports.Application.Common;
using MatchingSports.Domain.Entities;
using MatchingSports.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace MatchingSports.Tests;

public sealed class WebFoundationPersistenceTests
{
    [RbacDatabaseFact]
    public async Task MigrationPreservesExistingResourcesAndPreferencesRejectConcurrentUpdates()
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("MATCHINGSPORTS_RBAC_TEST_CONNECTION")!);
        Assert.StartsWith("matchingsports_rbac_test_", connection.Database, StringComparison.Ordinal);
        connection.Database = $"matchingsports_rbac_test_web_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<MatchingSportsDbContext>()
            .UseNpgsql(connection.ConnectionString, postgres => postgres.UseNetTopologySuite()).Options;
        var audit = new AuditActorContext();
        await using var db = new MatchingSportsDbContext(options, audit);
        try
        {
            var migrations = db.Database.GetMigrations().ToArray();
            await db.GetService<IMigrator>().MigrateAsync(migrations[^2]);
            var sportId = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO sports (id, code, name) VALUES ({sportId}, 'MIGRATION_TEST', 'Existing sport')");
            await db.Database.MigrateAsync();
            var sport = await db.Sports.SingleAsync(item => item.Id == sportId);
            Assert.Equal("Existing sport", sport.Name);
            Assert.Null(sport.Description);
            var userId = Guid.NewGuid();
            db.Users.Add(new User { Id = userId, Email = "preferences@example.test", UserName = "preferences@example.test" });
            var preferences = UserPreferences.Create(userId);
            preferences.Update("en", "dark", "purple");
            db.UserPreferences.Add(preferences);
            using (audit.BeginScope(userId))
                await db.SaveChangesAsync();
            Assert.Equal(userId, preferences.CreatedBy);
            Assert.Equal(userId, preferences.UpdatedBy);
            Assert.NotEqual(Guid.Empty, preferences.ConcurrencyToken);
            await using var competing = new MatchingSportsDbContext(options, audit);
            var stale = await competing.UserPreferences.SingleAsync(item => item.UserId == userId);
            var originalCreatedAt = preferences.CreatedAt;
            preferences.Update("vi", "light", "green");
            using (audit.BeginScope(userId))
                await db.SaveChangesAsync();
            stale.Update("en", "system", "blue");
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => competing.SaveChangesAsync());
            var persisted = await db.UserPreferences.AsNoTracking().SingleAsync(item => item.UserId == userId);
            Assert.Equal("green", persisted.ThemePreset);
            Assert.Equal(originalCreatedAt, persisted.CreatedAt);
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
