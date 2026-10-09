using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Matching;
using MatchingSports.Domain.Entities;

namespace MatchingSports.Application.Abstractions;

public interface IMatchingTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}
public interface IMatchingRepository
{
    Task<IMatchingTransaction> BeginWriteAsync(CancellationToken cancellationToken);
    void Add(Listing listing);
    void Add(ListingWatch watch);
    void TrackChildren(Listing listing);
    void AddEvent(OutboxMessage message);
    Task<Listing?> GetListingAsync(Guid id, bool tracked, CancellationToken cancellationToken);
    Task<Listing?> GetListingByMatchAsync(Guid matchId, bool tracked, CancellationToken cancellationToken);
    Task<ListingWatch?> GetWatchAsync(Guid id, bool tracked, CancellationToken cancellationToken);
    Task<bool> IsActiveFormatAsync(Guid sportId, Guid? formatId, CancellationToken cancellationToken);
    Task<int> CountActiveWatchesAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> TeamUsersAsync(IEnumerable<Guid> teamIds, bool managersOnly, CancellationToken cancellationToken);
    Task<bool> CanViewListingAsync(Guid listingId, Guid actorId, CancellationToken cancellationToken);
    Task<bool> CanViewMatchAsync(Guid matchId, Guid actorId, bool activeOnly, CancellationToken cancellationToken);
    Task<PaginatedResult<ListingSearchResult>> SearchAsync(GetListingsQuery query, DateTimeOffset now, CancellationToken cancellationToken);
    Task<PaginatedResult<ListingRequest>> ListRequestsAsync(GetListingRequestsQuery query, CancellationToken cancellationToken);
    Task<PaginatedResult<Match>> ListMatchesAsync(GetMatchesQuery query, CancellationToken cancellationToken);
    Task<PaginatedResult<ListingWatch>> ListWatchesAsync(GetListingWatchesQuery query, DateTimeOffset now, CancellationToken cancellationToken);
    Task<IReadOnlyList<MatchContactDto>> GetContactsAsync(Guid matchId, CancellationToken cancellationToken);
}

public interface IMatchingWorkRepository
{
    Task<IMatchingTransaction> BeginAsync(CancellationToken cancellationToken);
    Task<bool> TryLockWorkerAsync(CancellationToken cancellationToken);
    Task<OutboxMessage?> LockDueEventAsync(DateTimeOffset now, CancellationToken cancellationToken);
    Task<OutboxMessage?> LockEventAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> HasReconciliationAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ListingWatch>> FindWatchesAsync(Listing listing, Guid? afterId, int batchSize, DateTimeOffset now, CancellationToken cancellationToken);
    Task<IReadOnlyList<Listing>> FindListingsAsync(ListingWatch watch, Guid? afterId, int batchSize, DateTimeOffset now, CancellationToken cancellationToken);
    Task<IReadOnlyList<ListingWatch>> ActiveWatchBatchAsync(Guid? afterId, int batchSize, DateTimeOffset now, CancellationToken cancellationToken);
    Task<Listing?> LockListingForDeliveryAsync(Guid id, CancellationToken cancellationToken);
    Task<ListingWatch?> LockWatchForDeliveryAsync(Guid id, CancellationToken cancellationToken);
    Task<double> DistanceMetersAsync(ListingWatch watch, Guid listingId, CancellationToken cancellationToken);
    Task<bool> MatchesWatchAsync(ListingWatch watch, Guid listingId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<IReadOnlyList<ListingWatch>> EmailWatchesAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<bool> HasDeliveryAsync(Guid userId, Guid listingId, CancellationToken cancellationToken);
    void AddDelivery(ListingWatchDelivery delivery);
    Task<IReadOnlyList<Listing>> LockExpiredListingsAsync(DateTimeOffset now, int batchSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<ListingWatch>> LockExpiredWatchesAsync(DateTimeOffset now, int batchSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<Listing>> LockReminderListingsAsync(DateTimeOffset now, TimeSpan leadTime, int batchSize, CancellationToken cancellationToken);
    void DiscardPendingChanges();
}
