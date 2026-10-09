using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Matching;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Shared;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace MatchingSports.Infrastructure.Persistence.Repositories;

internal sealed partial class MatchingRepository
{
    // This transaction-scoped lock serializes worker reconciliation and delivery deduplication across hosts.
    public async Task<bool> TryLockWorkerAsync(CancellationToken cancellationToken) =>
        await db.Database.SqlQueryRaw<bool>("SELECT pg_try_advisory_xact_lock(741963854) AS \"Value\"").SingleAsync(cancellationToken);
    public async Task<OutboxMessage?> LockDueEventAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        (await db.OutboxMessages.FromSqlInterpolated($"""
            SELECT * FROM outbox_messages WHERE event_type IN ({MatchingEvents.ListingChanged}, {MatchingEvents.WatchChanged}, {MatchingEvents.Reconcile})
            AND processed_at IS NULL AND failed_at IS NULL AND available_at <= {now}
            AND (locked_until IS NULL OR locked_until <= {now})
            ORDER BY available_at, occurred_at, id LIMIT 1 FOR UPDATE SKIP LOCKED
            """).ToListAsync(cancellationToken)).SingleOrDefault();
    public async Task<OutboxMessage?> LockEventAsync(Guid id, CancellationToken cancellationToken) =>
        (await db.OutboxMessages.FromSqlInterpolated($"SELECT * FROM outbox_messages WHERE id = {id} FOR UPDATE")
            .ToListAsync(cancellationToken)).SingleOrDefault();
    public Task<bool> HasReconciliationAsync(CancellationToken cancellationToken) =>
        db.OutboxMessages.AnyAsync(item => item.EventType == MatchingEvents.Reconcile && item.ProcessedAt == null && item.FailedAt == null, cancellationToken);
    public async Task<IReadOnlyList<ListingWatch>> FindWatchesAsync(Listing listing, Guid? afterId, int batchSize, DateTimeOffset now, CancellationToken cancellationToken) =>
        await ActiveWatches(now).Where(watch => watch.SportId == listing.SportId && (watch.FormatId == null || watch.FormatId == listing.FormatId)
            && watch.StartFrom <= listing.StartAt && watch.StartTo >= listing.StartAt
            && !db.ListingWatchDeliveries.Any(delivery => delivery.UserId == watch.UserId && delivery.ListingId == listing.Id) && (afterId == null || watch.Id.CompareTo(afterId.Value) > 0))
            .OrderBy(watch => watch.Id).Take(batchSize).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<Listing>> FindListingsAsync(ListingWatch watch, Guid? afterId, int batchSize, DateTimeOffset now, CancellationToken cancellationToken) =>
        await ApplyCriteria(EligiblePlayers(watch.UserId, now), watch.GetCriteria())
            .Where(listing => (afterId == null || listing.Id.CompareTo(afterId.Value) > 0)
                && !db.ListingWatchDeliveries.Any(delivery => delivery.UserId == watch.UserId && delivery.ListingId == listing.Id))
            .OrderBy(listing => listing.Id).Take(batchSize).ToListAsync(cancellationToken);
    public async Task<Listing?> LockListingForDeliveryAsync(Guid id, CancellationToken cancellationToken) =>
        (await db.Listings.FromSqlInterpolated($"SELECT * FROM listings WHERE id = {id} AND deleted_at IS NULL FOR UPDATE")
            .IgnoreQueryFilters().ToListAsync(cancellationToken)).SingleOrDefault();
    public async Task<ListingWatch?> LockWatchForDeliveryAsync(Guid id, CancellationToken cancellationToken) =>
        (await db.ListingWatches.FromSqlInterpolated($"SELECT * FROM listing_watches WHERE id = {id} FOR UPDATE")
            .IgnoreQueryFilters().ToListAsync(cancellationToken)).SingleOrDefault();
    public Task<double> DistanceMetersAsync(ListingWatch watch, Guid listingId, CancellationToken cancellationToken)
    {
        var point = GeoPoint(watch.Latitude, watch.Longitude);
        return db.Listings.Where(listing => listing.Id == listingId)
            .Select(listing => EF.Property<Point>(listing, "LocationGeography").Distance(point)).SingleAsync(cancellationToken);
    }
    public Task<bool> MatchesWatchAsync(ListingWatch watch, Guid listingId, DateTimeOffset now, CancellationToken cancellationToken) =>
        ApplyCriteria(EligiblePlayers(watch.UserId, now), watch.GetCriteria()).AnyAsync(listing => listing.Id == listingId, cancellationToken);
    public async Task<IReadOnlyList<ListingWatch>> ActiveWatchBatchAsync(Guid? afterId, int batchSize, DateTimeOffset now, CancellationToken cancellationToken) =>
        await ActiveWatches(now).Where(watch => afterId == null || watch.Id.CompareTo(afterId.Value) > 0)
            .OrderBy(watch => watch.Id).Take(batchSize).ToListAsync(cancellationToken);
    private IQueryable<ListingWatch> ActiveWatches(DateTimeOffset now) => db.ListingWatches.Where(watch =>
        watch.Status == ListingWatchStatus.ACTIVE && watch.StartTo > now && watch.User.Status == UserStatus.ACTIVE);
    public async Task<IReadOnlyList<ListingWatch>> EmailWatchesAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken) =>
        await ActiveWatches(now).Where(watch => watch.UserId == userId && watch.EmailEnabled).ToListAsync(cancellationToken);
    public Task<bool> HasDeliveryAsync(Guid userId, Guid listingId, CancellationToken cancellationToken) =>
        db.ChangeTracker.Entries<ListingWatchDelivery>().Any(entry => entry.State == EntityState.Added
            && entry.Entity.UserId == userId && entry.Entity.ListingId == listingId)
            ? Task.FromResult(true)
            : db.ListingWatchDeliveries.AnyAsync(item => item.UserId == userId && item.ListingId == listingId, cancellationToken);
    public void AddDelivery(ListingWatchDelivery delivery) => db.ListingWatchDeliveries.Add(delivery);
    public async Task<IReadOnlyList<Listing>> LockExpiredListingsAsync(DateTimeOffset now, int batchSize, CancellationToken cancellationToken)
    {
        var locked = await db.Listings.FromSqlInterpolated($"""
            SELECT * FROM listings WHERE deleted_at IS NULL AND ((status = 'OPEN' AND cutoff_at <= {now}) OR (status = 'MATCHED' AND start_at <= {now}))
            ORDER BY cutoff_at, id LIMIT {batchSize} FOR UPDATE SKIP LOCKED
            """).IgnoreQueryFilters().ToListAsync(cancellationToken);
        var ids = locked.Select(item => item.Id).ToArray();
        return await db.Listings.IgnoreQueryFilters().Where(item => ids.Contains(item.Id)).Include(item => item.Requests)
            .Include(item => item.Match!).ThenInclude(item => item.Participants).AsSplitQuery().ToListAsync(cancellationToken);
    }
    public async Task<IReadOnlyList<ListingWatch>> LockExpiredWatchesAsync(DateTimeOffset now, int batchSize, CancellationToken cancellationToken) =>
        await db.ListingWatches.FromSqlInterpolated($"""
            SELECT * FROM listing_watches WHERE status IN ('ACTIVE','PAUSED') AND start_to <= {now}
            ORDER BY start_to, id LIMIT {batchSize} FOR UPDATE SKIP LOCKED
            """).IgnoreQueryFilters().ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<Listing>> LockReminderListingsAsync(DateTimeOffset now, TimeSpan leadTime, int batchSize, CancellationToken cancellationToken)
    {
        try
        {
            var through = now + leadTime;
            // Lock both aggregate rows: multiple hosts cannot publish the same reminder or race cancellation.
            var locked = await db.Listings.FromSqlInterpolated($"""
                SELECT l.* FROM listings l JOIN matches m ON m.listing_id = l.id
                WHERE l.deleted_at IS NULL AND l.status = 'CONFIRMED' AND m.status = 'CONFIRMED'
                    AND m.reminder_published_at IS NULL AND m.start_at > {now} AND m.start_at <= {through}
                ORDER BY m.start_at, m.id LIMIT {batchSize} FOR UPDATE OF l, m SKIP LOCKED
                """).ToListAsync(cancellationToken);
            var ids = locked.Select(listing => listing.Id).ToArray();
            return await ListingGraph(true).Where(listing => ids.Contains(listing.Id)).OrderBy(listing => listing.StartAt)
                .ThenBy(listing => listing.Id).ToListAsync(cancellationToken);
        }
        catch (Exception exception) when (PersistenceTransactionConflicts.IsConflict(exception))
        {
            // Workers run outside MediatR, so locking failures also need the stable concurrency contract.
            throw new ConflictException(ErrorCodes.ConcurrencyConflict, "Có thao tác đồng thời. Vui lòng thử lại ở lượt worker tiếp theo.");
        }
    }

    public void DiscardPendingChanges() => db.ChangeTracker.Clear();
}
