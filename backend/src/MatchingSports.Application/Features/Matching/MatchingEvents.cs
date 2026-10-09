using System.Text.Json;
using MatchingSports.Domain.Entities;
using MatchingSports.Shared;

namespace MatchingSports.Application.Features.Matching;

public static class MatchingEvents
{
    public const string ListingChanged = "ListingAvailabilityChanged";
    public const string WatchChanged = "ListingWatchCriteriaChanged";
    public const string Reconcile = "ListingWatchesReconcile";
    public const string WatchAggregate = "LISTING_WATCH";
    public sealed record Cursor(Guid? AfterId = null);
    public static OutboxMessage Create(string eventType, Guid id, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        AggregateType = eventType == ListingChanged ? EntityTypes.Listing : WatchAggregate,
        AggregateId = id,
        EventType = eventType,
        PayloadJson = JsonSerializer.Serialize(new Cursor()),
        OccurredAt = now,
        AvailableAt = now
    };
}
