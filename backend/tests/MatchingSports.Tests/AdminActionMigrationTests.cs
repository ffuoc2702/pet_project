using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace MatchingSports.Tests;

public sealed class AdminActionMigrationTests
{
    [RbacDatabaseFact]
    public async Task MergeAndRollbackPreserveAuthorizationHistoryAndExistingModeration()
    {
        var connection = new NpgsqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("MATCHINGSPORTS_RBAC_TEST_CONNECTION")!);
        Assert.StartsWith("matchingsports_rbac_test_", connection.Database, StringComparison.Ordinal);
        // Each migration test owns a separate disposable database.
        connection.Database = $"matchingsports_rbac_test_audit_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<MatchingSportsDbContext>()
            .UseNpgsql(connection.ConnectionString, postgres => postgres.UseNetTopologySuite()).Options;
        await using var db = new MatchingSportsDbContext(options);
        const string previousMigration = "20261006023325_LinkUserSportProfileFormats";
        var migrator = db.GetService<IMigrator>();
        try
        {
            await migrator.MigrateAsync(previousMigration);
            var actor = User.CreateVerifiedAccount("audit@example.test", "Audit actor");
            actor.NormalizedEmail = actor.Email!.ToUpperInvariant();
            actor.NormalizedUserName = actor.UserName!.ToUpperInvariant();
            db.Users.Add(actor);
            await db.SaveChangesAsync();
            var moderationId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var createdAt = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO admin_actions (id, admin_user_id, action_type, target_type, target_id, reason)
                VALUES ({moderationId}, {actor.Id}, 'WARN_USER', 'USER', {targetId}, 'Existing reason');
                """);
            var actions = new[] { "roles.create", "roles.update", "roles.permissions.update", "roles.assign" };
            var types = new[] { AdminActionType.CREATE_ROLE, AdminActionType.UPDATE_ROLE,
                AdminActionType.UPDATE_ROLE_PERMISSIONS, AdminActionType.ASSIGN_USER_ROLES };
            var ids = actions.Select(_ => Guid.NewGuid()).ToArray();
            for (var i = 0; i < actions.Length; i++)
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO authorization_audit_entries
                        (id, actor_user_id, action, target_id, before_json, after_json, created_at)
                    VALUES ({ids[i]}, {actor.Id}, {actions[i]}, {targetId}, 'null'::jsonb, '[1,2]'::jsonb, {createdAt});
                    """);
            }

            var unsupportedId = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO authorization_audit_entries
                    (id, actor_user_id, action, target_id, before_json, after_json, created_at)
                VALUES ({unsupportedId}, {actor.Id}, 'unknown.action', {targetId}, 'null'::jsonb, 'null'::jsonb, {createdAt});
                """);
            await Assert.ThrowsAsync<PostgresException>(() => migrator.MigrateAsync());
            Assert.Equal(5, await db.Database.SqlQueryRaw<int>(
                "SELECT COUNT(*)::int AS \"Value\" FROM authorization_audit_entries").SingleAsync());
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM authorization_audit_entries WHERE id = {unsupportedId}");

            await migrator.MigrateAsync();
            var merged = await db.AdminActions.AsNoTracking().ToListAsync();
            Assert.Equal(5, merged.Count);
            for (var i = 0; i < actions.Length; i++)
            {
                var entry = Assert.Single(merged, entry => entry.Id == ids[i]);
                Assert.Equal(actor.Id, entry.AdminUserId);
                Assert.Equal(types[i], entry.ActionType);
                Assert.Equal(i == 3 ? "USER" : "ROLE", entry.TargetType);
                Assert.Equal(targetId, entry.TargetId);
                Assert.Equal("null", entry.BeforeJson);
                Assert.Equal("[1, 2]", entry.AfterJson);
                Assert.Equal(createdAt, entry.CreatedAt);
                Assert.Null(entry.Reason);
            }
            var moderation = Assert.Single(merged, entry => entry.Id == moderationId);
            Assert.Equal("Existing reason", moderation.Reason);
            Assert.Equal("{}", moderation.BeforeJson);
            Assert.Equal("{}", moderation.AfterJson);
            Assert.Null(await db.Database.SqlQueryRaw<string>(
                "SELECT to_regclass('authorization_audit_entries')::text AS \"Value\"").SingleAsync());

            actor.DeletedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
            Assert.Equal(5, await db.AdminActions.CountAsync());

            await migrator.MigrateAsync(previousMigration);
            for (var i = 0; i < actions.Length; i++)
            {
                Assert.Equal(actions[i], await db.Database.SqlQuery<string>(
                    $"SELECT action AS \"Value\" FROM authorization_audit_entries WHERE id = {ids[i]}").SingleAsync());
                Assert.Equal("[1, 2]", await db.Database.SqlQuery<string>(
                    $"SELECT after_json::text AS \"Value\" FROM authorization_audit_entries WHERE id = {ids[i]}").SingleAsync());
            }
            Assert.Equal(1, await db.Database.SqlQueryRaw<int>(
                "SELECT COUNT(*)::int AS \"Value\" FROM admin_actions").SingleAsync());
            await migrator.MigrateAsync();
            Assert.Equal(5, await db.AdminActions.CountAsync());
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }
}
