using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Features.Notification;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace MatchingSports.Infrastructure.Persistence.Repositories;

internal sealed class NotificationWorkRepository(MatchingSportsDbContext db) : INotificationWorkRepository
{
    public async Task<INotificationTransaction> BeginAsync(CancellationToken cancellationToken, bool repeatableRead = false) =>
        new NotificationTransaction(await db.Database.BeginTransactionAsync(repeatableRead
            ? System.Data.IsolationLevel.RepeatableRead : System.Data.IsolationLevel.ReadCommitted, cancellationToken));

    public async Task<NotificationConfig?> LockDueConfigAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var config = await db.NotificationConfigs.FromSqlInterpolated($"""
            SELECT * FROM notification_configs
            WHERE status = 'ACTIVE' AND scheduling_status IN ('PENDING', 'SCHEDULING')
                AND next_run_at <= {now} AND (locked_until IS NULL OR locked_until <= {now})
            ORDER BY next_run_at, id LIMIT 1 FOR UPDATE SKIP LOCKED
            """).FirstOrDefaultAsync(cancellationToken);
        if (config is not null) await LoadTargetsAsync(config, cancellationToken);
        return config;
    }

    public async Task<NotificationConfig?> LockConfigAsync(Guid id, CancellationToken cancellationToken)
    {
        var config = await db.NotificationConfigs.FromSqlInterpolated($"SELECT * FROM notification_configs WHERE id = {id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (config is not null)
        {
            // Claim and execution use separate transactions; refresh any entity tracked during the claim.
            await db.Entry(config).ReloadAsync(cancellationToken);
            await LoadTargetsAsync(config, cancellationToken);
        }
        return config;
    }

    private async Task LoadTargetsAsync(NotificationConfig config, CancellationToken cancellationToken)
    {
        await db.Entry(config).Collection(x => x.Users).LoadAsync(cancellationToken);
        await db.Entry(config).Collection(x => x.Teams).LoadAsync(cancellationToken);
    }

    public Task<IReadOnlyCollection<NotificationRecipient>> GetRecipientsAsync(NotificationConfigValues values, CancellationToken cancellationToken) =>
        new NotificationRepository(db).MaterializeRecipientsAsync(values, null, cancellationToken);

    public void AddOccurrence(NotificationHistory history, OutboxMessage? email)
    {
        db.NotificationHistories.Add(history);
        if (email is not null) db.OutboxMessages.Add(email);
    }

    public Task<OutboxMessage?> LockDueMessageAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        db.OutboxMessages.FromSqlInterpolated($"""
            SELECT * FROM outbox_messages
            WHERE event_type = {NotificationJobs.EmailEvent} AND processed_at IS NULL AND failed_at IS NULL
                AND available_at <= {now} AND (locked_until IS NULL OR locked_until <= {now})
            ORDER BY available_at, id LIMIT 1 FOR UPDATE SKIP LOCKED
            """).FirstOrDefaultAsync(cancellationToken);

    public Task<NotificationHistory?> GetHistoryForDeliveryAsync(Guid id, CancellationToken cancellationToken) =>
        db.NotificationHistories.FromSqlInterpolated($"SELECT * FROM notification_history WHERE id = {id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    public Task<Match?> LockReminderMatchAsync(Guid matchId, CancellationToken cancellationToken) =>
        db.Matches.FromSqlInterpolated($"SELECT * FROM matches WHERE id = {matchId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    public Task<SystemSettings> GetSettingsAsync(CancellationToken cancellationToken) => db.SystemSettings.AsNoTracking().SingleAsync(cancellationToken);

    public async Task<NotificationRecipient?> GetRecipientAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking().Where(x => x.Id == userId && x.Status == UserStatus.ACTIVE)
            .Select(x => new NotificationRecipient(x.Id, x.Email, x.EmailConfirmed, Array.Empty<Guid>())).SingleOrDefaultAsync(cancellationToken);

    public async Task<int> PurgeBatchAsync(DateTimeOffset now, int batchSize, CancellationToken cancellationToken)
    {
        if (batchSize is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(batchSize));
        // Cleanup deletes are intentionally outside SaveChanges. Pending deliveries keep their history.
        var historyCount = await db.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM notification_history h USING (
                SELECT id FROM notification_history history
                WHERE purge_after <= {now} AND NOT EXISTS (
                    SELECT 1 FROM outbox_messages o
                    WHERE o.event_type = {NotificationJobs.EmailEvent}
                        AND o.payload_json->>'HistoryId' = history.id::text
                        AND o.processed_at IS NULL AND o.failed_at IS NULL)
                ORDER BY purge_after, id LIMIT {batchSize} FOR UPDATE SKIP LOCKED
            ) expired WHERE h.id = expired.id
            """, cancellationToken);
        var messageCount = await db.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM outbox_messages o USING (
                SELECT id FROM outbox_messages WHERE processed_at IS NOT NULL AND purge_after <= {now}
                ORDER BY purge_after, id LIMIT {batchSize} FOR UPDATE SKIP LOCKED
            ) expired WHERE o.id = expired.id
            """, cancellationToken);
        return historyCount + messageCount;
    }

    private sealed class NotificationTransaction(IDbContextTransaction transaction) : INotificationTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken) => transaction.CommitAsync(cancellationToken);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
