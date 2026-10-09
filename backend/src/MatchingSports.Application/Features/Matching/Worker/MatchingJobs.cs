using System.Text.Json;
using MatchingSports.Application.Abstractions;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Shared;

namespace MatchingSports.Application.Features.Matching;

public sealed class MatchingJobs(IMatchingRepository repository, IMatchingWorkRepository work, MatchingServices services,
    MatchingNotifications notifications, IUnitOfWork unitOfWork, IAuditActorContext actorContext, TimeProvider clock)
{
    private const int BatchSize = 100;
    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        using var actorScope = actorContext.BeginScope(null);
        Guid? messageId = null;
        try
        {
            await using var transaction = await work.BeginAsync(cancellationToken);
            if (!await work.TryLockWorkerAsync(cancellationToken)) return false;
            var now = clock.GetUtcNow();
            var message = await work.LockDueEventAsync(now, cancellationToken);
            if (message is null)
            {
                if (await work.HasReconciliationAsync(cancellationToken)) return false;
                repository.AddEvent(MatchingEvents.Create(MatchingEvents.Reconcile, Domain.Entities.SystemSettings.SingletonId, now));
                await services.SaveAsync(null, transaction, cancellationToken);
                return true;
            }
            messageId = message.Id;
            MatchingEvents.Cursor cursor;
            try
            {
                cursor = JsonSerializer.Deserialize<MatchingEvents.Cursor>(message.PayloadJson) ?? throw new JsonException();
                if (message.AggregateId == Guid.Empty || (message.EventType == MatchingEvents.ListingChanged
                    ? message.AggregateType != EntityTypes.Listing : message.AggregateType != MatchingEvents.WatchAggregate)) throw new JsonException();
            }
            catch (Exception exception) when (exception is JsonException or NotSupportedException)
            {
                message.FailPermanently("INVALID_MATCHING_PAYLOAD", now);
                await services.SaveAsync(null, transaction, cancellationToken);
                return true;
            }
            if (message.EventType == MatchingEvents.ListingChanged)
            {
                var listing = await repository.GetListingAsync(message.AggregateId, true, cancellationToken);
                if (listing is null || !listing.IsAvailable(now) || listing.Mode != ListingMode.FIND_PLAYERS) message.MarkProcessed(now);
                else
                {
                    var watches = await work.FindWatchesAsync(listing, cursor.AfterId, BatchSize, now, cancellationToken);
                    foreach (var watch in watches)
                        if (await work.MatchesWatchAsync(watch, listing.Id, now, cancellationToken))
                            await DeliverAsync(listing, watch, cancellationToken);
                    Advance(message, watches.Select(watch => watch.Id).ToArray(), now);
                }
            }
            else if (message.EventType == MatchingEvents.WatchChanged)
            {
                var watch = await repository.GetWatchAsync(message.AggregateId, true, cancellationToken);
                if (watch is null || !watch.IsActive(now)) message.MarkProcessed(now);
                else
                {
                    var listings = await work.FindListingsAsync(watch, cursor.AfterId, BatchSize, now, cancellationToken);
                    foreach (var listing in listings) await DeliverAsync(listing, watch, cancellationToken);
                    Advance(message, listings.Select(listing => listing.Id).ToArray(), now);
                }
            }
            else
            {
                var watches = await work.ActiveWatchBatchAsync(cursor.AfterId, BatchSize, now, cancellationToken);
                foreach (var watch in watches) repository.AddEvent(MatchingEvents.Create(MatchingEvents.WatchChanged, watch.Id, now));
                Advance(message, watches.Select(watch => watch.Id).ToArray(), now);
                if (message.ProcessedAt is not null)
                {
                    var nextSweep = MatchingEvents.Create(MatchingEvents.Reconcile, Domain.Entities.SystemSettings.SingletonId, now);
                    nextSweep.AvailableAt = now.AddMinutes(1);
                    repository.AddEvent(nextSweep);
                }
            }
            await services.SaveAsync(null, transaction, cancellationToken);
            return true;
        }
        catch (Exception exception) when (messageId is not null && exception is not OperationCanceledException)
        {
            // Rollback discards incomplete notifications; retry stores only a safe code, never exception content.
            work.DiscardPendingChanges();
            await using var failureTransaction = await work.BeginAsync(cancellationToken);
            var message = await work.LockEventAsync(messageId.Value, cancellationToken);
            if (message is not null && message.ProcessedAt is null && message.FailedAt is null)
                message.RecordDeliveryFailure("MATCHING_PROCESSING_FAILED", await services.SettingsAsync(cancellationToken), clock.GetUtcNow());
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await failureTransaction.CommitAsync(cancellationToken);
            return true;
        }
    }
    private async Task DeliverAsync(Listing listing, ListingWatch watch, CancellationToken cancellationToken)
    {
        // Serialize publishing with pause/cancel and with listing closure; in-flight publication commits first.
        var lockedListing = await work.LockListingForDeliveryAsync(listing.Id, cancellationToken);
        var lockedWatch = await work.LockWatchForDeliveryAsync(watch.Id, cancellationToken);
        if (lockedListing is null || lockedWatch is null) return;
        listing = lockedListing; watch = lockedWatch;
        if (!watch.IsActive(clock.GetUtcNow()) || !listing.IsAvailable(clock.GetUtcNow())
            || !await work.MatchesWatchAsync(watch, listing.Id, clock.GetUtcNow(), cancellationToken)
            || await work.HasDeliveryAsync(watch.UserId, listing.Id, cancellationToken)) return;
        var sendEmail = watch.EmailEnabled;
        if (!sendEmail)
            foreach (var emailWatch in await work.EmailWatchesAsync(watch.UserId, clock.GetUtcNow(), cancellationToken))
                if (await work.MatchesWatchAsync(emailWatch, listing.Id, clock.GetUtcNow(), cancellationToken)) { sendEmail = true; break; }
        var historyId = await notifications.PublishWatchAsync(listing, watch, sendEmail, await work.DistanceMetersAsync(watch, listing.Id, cancellationToken), cancellationToken);
        if (historyId is { } id) work.AddDelivery(ListingWatchDelivery.Create(watch.UserId, listing.Id, watch.Id, id));
    }
    private static void Advance(OutboxMessage message, Guid[] ids, DateTimeOffset now)
    {
        if (ids.Length < BatchSize) message.MarkProcessed(now);
        else
        {
            message.PayloadJson = JsonSerializer.Serialize(new MatchingEvents.Cursor(ids[^1]));
            message.AvailableAt = now;
        }
    }
    public async Task<int> PublishRemindersBatchAsync(CancellationToken cancellationToken)
    {
        using var actorScope = actorContext.BeginScope(null);
        await using var transaction = await work.BeginAsync(cancellationToken);
        var settings = await services.SettingsAsync(cancellationToken);
        var leadTime = TimeSpan.FromMinutes(settings.MatchReminderLeadMinutes);
        var listings = await work.LockReminderListingsAsync(clock.GetUtcNow(), leadTime, 50, cancellationToken);
        var published = 0;
        foreach (var listing in listings)
        {
            var now = clock.GetUtcNow();
            if (listing.Match is not { } match || !match.IsReminderDue(now, leadTime)) continue;
            await notifications.PublishAsync(listing, null, NotificationType.MATCH_REMINDER,
                await notifications.ParticipantsAsync(listing, cancellationToken), cancellationToken);
            match.RecordReminderPublished(now, leadTime);
            published++;
        }
        await services.SaveAsync(null, transaction, cancellationToken);
        return published;
    }

    public async Task<int> ExpireBatchAsync(CancellationToken cancellationToken)
    {
        using var actorScope = actorContext.BeginScope(null);
        await using var transaction = await work.BeginAsync(cancellationToken);
        var now = clock.GetUtcNow();
        var listings = await work.LockExpiredListingsAsync(now, 50, cancellationToken);
        foreach (var listing in listings)
        {
            var affected = listing.Requests.Where(request => request.Status is RequestStatus.PENDING or RequestStatus.ACCEPTED).ToArray();
            var recipients = (await notifications.ParticipantsAsync(listing, cancellationToken))
                .Concat(await notifications.ApplicantsAsync(affected, cancellationToken)).ToArray();
            if (listing.Expire(now))
                await notifications.PublishAsync(listing, null, listing.Match is null ? NotificationType.REQUEST_EXPIRED : NotificationType.MATCH_CANCELLED,
                    recipients, cancellationToken);
        }
        var watches = await work.LockExpiredWatchesAsync(now, 50, cancellationToken);
        foreach (var watch in watches) watch.Expire(now);
        await services.SaveAsync(null, transaction, cancellationToken);
        return listings.Count + watches.Count;
    }
}
