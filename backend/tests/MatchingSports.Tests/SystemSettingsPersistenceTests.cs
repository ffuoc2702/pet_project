using MatchingSports.Application;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.SystemSettings;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Infrastructure;
using MatchingSports.Infrastructure.Persistence;
using MatchingSports.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace MatchingSports.Tests;

public sealed class SystemSettingsPersistenceTests
{
    [RbacDatabaseFact]
    public async Task MigrationBackfillsRetentionAndConcurrentEditsRollBackSettingsAndAudit()
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("MATCHINGSPORTS_RBAC_TEST_CONNECTION")!);
        Assert.StartsWith("matchingsports_rbac_test_", connection.Database, StringComparison.Ordinal);
        connection.Database = $"matchingsports_rbac_test_settings_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<MatchingSportsDbContext>()
            .UseNpgsql(connection.ConnectionString, postgres => postgres.UseNetTopologySuite()).Options;
        await using var db = new MatchingSportsDbContext(options);
        var migrator = db.GetService<IMigrator>();
        const string previousMigration = "20261006082428_MergeAuthorizationAuditIntoAdminActions";
        try
        {
            await migrator.MigrateAsync(previousMigration);
            var actor = User.CreateVerifiedAccount("settings@example.test", "Settings admin");
            actor.NormalizedEmail = actor.Email!.ToUpperInvariant();
            actor.NormalizedUserName = actor.UserName!.ToUpperInvariant();
            await MigrationTestData.InsertUserAsync(db, actor);
            var oldCreatedAt = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
            var oldMessageId = Guid.NewGuid();
            var oldNotificationId = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($$"""
                INSERT INTO outbox_messages (id, aggregate_type, aggregate_id, event_type, payload_json, created_at)
                VALUES ({{oldMessageId}}, 'TEAM', {{actor.Id}}, 'TestEvent', '{}'::jsonb, {{oldCreatedAt}})
                """);
            await db.Database.ExecuteSqlInterpolatedAsync($$"""
                INSERT INTO notifications (id, user_id, type, title, body, created_at)
                VALUES ({{oldNotificationId}}, {{actor.Id}}, 'MATCH_REMINDER', 'Reminder', 'Details', {{oldCreatedAt}})
                """);
            await migrator.MigrateAsync();
            var migratedMessage = await db.OutboxMessages.SingleAsync();
            var migratedNotification = await db.Notifications.SingleAsync();
            Assert.Equal(oldCreatedAt.AddDays(30), migratedMessage.PurgeAfter);
            Assert.Equal(oldCreatedAt.AddDays(90), migratedNotification.PurgeAfter);
            Assert.Equal(oldCreatedAt, migratedMessage.CreatedAt);
            Assert.Equal(oldCreatedAt, migratedMessage.UpdatedAt);
            Assert.Null(migratedMessage.CreatedBy);
            Assert.Null(migratedMessage.UpdatedBy);
            Assert.Equal(oldCreatedAt, migratedNotification.CreatedAt);
            Assert.Equal(oldCreatedAt, migratedNotification.UpdatedAt);
            Assert.Null(migratedNotification.CreatedBy);
            Assert.Null(migratedNotification.UpdatedBy);
            Assert.Single(await db.SystemSettings.ToListAsync());

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:matchingsports"] = connection.ConnectionString,
                ["Auth:Jwt:SigningKey"] = new string('s', 48),
                ["Auth:Otp:Pepper"] = new string('p', 48)
            }).Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddApplication();
            services.AddInfrastructure(configuration);
            services.AddSingleton<IUserRoleRepository>(new TestActorRepository(actor.Id));
            await using var provider = services.BuildServiceProvider();

            Guid version;
            await using (var scope = provider.CreateAsyncScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var settings = await mediator.Send(new GetSystemSettingsQuery(actor.Id));
                var updated = await mediator.Send(new UpdateSystemSettingsCommand(actor.Id, settings.ConcurrencyToken,
                    new SystemSettingsInput(2, 3, 10, 2, 50, 24, 20, 5, 60, 7, 14)));
                Assert.NotEqual(settings.ConcurrencyToken, updated.ConcurrencyToken);
                version = updated.ConcurrencyToken;
                var context = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
                Assert.Equal(version, (await context.SystemSettings.AsNoTracking().SingleAsync()).ConcurrencyToken);
                var notification = new Notification
                {
                    UserId = actor.Id,
                    Type = NotificationType.MATCH_REMINDER,
                    Title = "New",
                    Body = "Details"
                };
                var message = NewMessage(actor.Id);
                context.AddRange(notification, message);
                context.Entry(notification).Property(item => item.CreatedAt).CurrentValue = oldCreatedAt;
                await context.SaveChangesAsync();
                // SaveChanges stamps CreatedAt; the expiry must use that final value, not the caller's value.
                Assert.Equal(notification.CreatedAt.AddDays(14), notification.PurgeAfter);
                Assert.Equal(message.CreatedAt.AddDays(7), message.PurgeAfter);
                Assert.NotEqual(oldCreatedAt, notification.CreatedAt);
                Assert.Equal(oldCreatedAt.AddDays(90), (await context.Notifications.SingleAsync(item => item.Id == oldNotificationId)).PurgeAfter);
                message.MarkProcessed(message.CreatedAt.AddDays(1));
                await context.SaveChangesAsync();
                Assert.Equal(message.CreatedAt.AddDays(7), message.PurgeAfter);
                var purgeable = await context.OutboxMessages.Where(item => item.ProcessedAt != null && item.PurgeAfter <= message.CreatedAt.AddDays(10)).ToListAsync();
                Assert.Equal(message.Id, Assert.Single(purgeable).Id);

                var syncMessage = NewMessage(actor.Id);
                context.Add(syncMessage);
                context.SaveChanges();
                Assert.Equal(syncMessage.CreatedAt.AddDays(7), syncMessage.PurgeAfter);
            }

            await using (var first = provider.CreateAsyncScope())
            await using (var second = provider.CreateAsyncScope())
            {
                var firstDb = first.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
                var secondDb = second.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
                await firstDb.SystemSettings.SingleAsync();
                await secondDb.SystemSettings.SingleAsync();
                // An unchanged submission still rotates the version and protects its audit from stale writers.
                var input = new SystemSettingsInput(2, 3, 10, 2, 50, 24, 20, 5, 60, 7, 14);
                var unchanged = await first.ServiceProvider.GetRequiredService<IMediator>().Send(new UpdateSystemSettingsCommand(actor.Id, version, input));
                Assert.NotEqual(version, unchanged.ConcurrencyToken);
                var error = await Assert.ThrowsAsync<ConflictException>(() => second.ServiceProvider.GetRequiredService<IMediator>().Send(
                    new UpdateSystemSettingsCommand(actor.Id, version, input with { NotificationRetentionDays = 16 })));
                Assert.Equal(ErrorCodes.ConcurrencyConflict, error.Code);
            }

            db.ChangeTracker.Clear();
            Assert.Equal(14, (await db.SystemSettings.SingleAsync()).NotificationRetentionDays);
            Assert.Equal(2, await db.AdminActions.CountAsync());
            Assert.Equal(oldCreatedAt.AddDays(90), (await db.Notifications.SingleAsync(item => item.Id == oldNotificationId)).PurgeAfter);
            await migrator.MigrateAsync(previousMigration);
            await migrator.MigrateAsync();
            db.ChangeTracker.Clear();
            Assert.Single(await db.SystemSettings.ToListAsync());
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public void RetentionIndexesAndSettingsConcurrencyAreConfigured()
    {
        var options = new DbContextOptionsBuilder<MatchingSportsDbContext>()
            .UseNpgsql("Host=localhost;Database=model_validation;Username=postgres;Password=postgres", postgres => postgres.UseNetTopologySuite()).Options;
        using var db = new MatchingSportsDbContext(options);
        var settings = db.Model.FindEntityType(typeof(SystemSettings))!;
        Assert.True(settings.FindProperty(nameof(SystemSettings.ConcurrencyToken))!.IsConcurrencyToken);
        var outbox = db.Model.FindEntityType(typeof(OutboxMessage))!;
        var purgeIndex = Assert.Single(outbox.GetIndexes(), index => index.GetDatabaseName() == "ix_outbox_purge_after");
        Assert.Equal("processed_at IS NOT NULL", purgeIndex.GetFilter());
        var pendingIndex = Assert.Single(outbox.GetIndexes(), index => index.GetDatabaseName() == "ix_outbox_pending");
        Assert.Equal("processed_at IS NULL AND failed_at IS NULL", pendingIndex.GetFilter());
        Assert.False(outbox.FindProperty(nameof(OutboxMessage.PurgeAfter))!.IsNullable);
        var notification = db.Model.FindEntityType(typeof(Notification))!;
        Assert.False(notification.FindProperty(nameof(Notification.PurgeAfter))!.IsNullable);
        Assert.Contains(notification.GetIndexes(), index => index.GetDatabaseName() == "ix_notifications_purge_after");
    }

    private static OutboxMessage NewMessage(Guid aggregateId) => new()
    {
        AggregateType = "TEAM",
        AggregateId = aggregateId,
        EventType = "TestEvent",
        PayloadJson = "{}",
        OccurredAt = DateTimeOffset.UtcNow,
        AvailableAt = DateTimeOffset.UtcNow
    };

    private sealed class TestActorRepository(Guid actorId) : IUserRoleRepository
    {
        public Task<UserRoleAssignment?> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<UserRoleAssignment?>(userId == actorId ? new UserRoleAssignment(actorId, [RoleNames.Admin],
                [PermissionCodes.ReadSystemSettings, PermissionCodes.UpdateSystemSettings]) : null);
        public Task ReplaceUserRolesAsync(UserRoleAssignment assignment, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
