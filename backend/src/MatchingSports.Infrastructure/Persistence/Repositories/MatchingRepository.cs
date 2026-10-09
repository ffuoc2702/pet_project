using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Matching;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NetTopologySuite.Geometries;

namespace MatchingSports.Infrastructure.Persistence.Repositories;

internal sealed partial class MatchingRepository(MatchingSportsDbContext db) : IMatchingRepository, IMatchingWorkRepository
{
    public async Task<IMatchingTransaction> BeginWriteAsync(CancellationToken cancellationToken) =>
        new MatchingTransaction(await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken));
    public Task<IMatchingTransaction> BeginAsync(CancellationToken cancellationToken) => BeginWriteAsync(cancellationToken);
    public void Add(Listing listing) => db.Listings.Add(listing);
    public void Add(ListingWatch watch) => db.ListingWatches.Add(watch);
    public void AddEvent(OutboxMessage message) => db.OutboxMessages.Add(message);
    public void TrackChildren(Listing listing)
    {
        var autoDetect = db.ChangeTracker.AutoDetectChangesEnabled;
        try
        {
            db.ChangeTracker.AutoDetectChangesEnabled = false;
            foreach (var request in listing.Requests.Where(item => db.Entry(item).State == EntityState.Detached)) db.ListingRequests.Add(request);
            if (listing.Match is { } match)
            {
                if (db.Entry(match).State == EntityState.Detached) db.Matches.Add(match);
                foreach (var participant in match.Participants.Where(item => db.Entry(item).State == EntityState.Detached)) db.MatchParticipants.Add(participant);
            }
        }
        finally { db.ChangeTracker.AutoDetectChangesEnabled = autoDetect; }
    }
    private IQueryable<Listing> ListingGraph(bool tracked) => (tracked ? db.Listings : db.Listings.AsNoTracking())
        .Include(item => item.Requests).Include(item => item.Match!).ThenInclude(item => item.Participants).AsSplitQuery();
    public Task<Listing?> GetListingAsync(Guid id, bool tracked, CancellationToken cancellationToken) =>
        ListingGraph(tracked).SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    public Task<Listing?> GetListingByMatchAsync(Guid matchId, bool tracked, CancellationToken cancellationToken) =>
        ListingGraph(tracked).SingleOrDefaultAsync(item => item.Match != null && item.Match.Id == matchId, cancellationToken);
    public Task<ListingWatch?> GetWatchAsync(Guid id, bool tracked, CancellationToken cancellationToken) =>
        (tracked ? db.ListingWatches : db.ListingWatches.AsNoTracking()).SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    public Task<bool> IsActiveFormatAsync(Guid sportId, Guid? formatId, CancellationToken cancellationToken) =>
        db.Sports.AnyAsync(sport => sport.Id == sportId && sport.IsActive && (formatId == null
            || sport.Formats.Any(format => format.Id == formatId && format.IsActive)), cancellationToken);
    public Task<int> CountActiveWatchesAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken) =>
        db.ListingWatches.CountAsync(item => item.UserId == userId && item.Status == ListingWatchStatus.ACTIVE && item.StartTo > now, cancellationToken);
    public async Task<IReadOnlyList<Guid>> TeamUsersAsync(IEnumerable<Guid> teamIds, bool managersOnly, CancellationToken cancellationToken)
    {
        var ids = teamIds.Distinct().ToArray();
        return await db.TeamMembers.Where(item => ids.Contains(item.TeamId) && item.Status == MemberStatus.ACTIVE
            && item.User.Status == UserStatus.ACTIVE && (!managersOnly || item.Role == TeamRole.CAPTAIN || item.Role == TeamRole.DEPUTY))
            .Select(item => item.UserId).Distinct().ToListAsync(cancellationToken);
    }
    public Task<bool> CanViewListingAsync(Guid listingId, Guid actorId, CancellationToken cancellationToken) =>
        db.Listings.AnyAsync(listing => listing.Id == listingId &&
            ((listing.HostTeamId == null && listing.OwnerUserId == actorId)
            || (listing.HostTeam != null && listing.HostTeam.Members.Any(member => member.UserId == actorId && member.Status == MemberStatus.ACTIVE))
            || listing.Requests.Any(request => request.ApplicantUserId == actorId ||
                (request.ApplicantTeam != null && request.ApplicantTeam.Members.Any(member => member.UserId == actorId && member.Status == MemberStatus.ACTIVE)))), cancellationToken);
    private IQueryable<Match> VisibleMatches(Guid actorId, bool activeOnly = false) => db.Matches.Where(match =>
        (match.Listing.HostTeamId == null && match.Listing.OwnerUserId == actorId)
        || (match.Listing.HostTeam != null && match.Listing.HostTeam.Members.Any(member => member.UserId == actorId && member.Status == MemberStatus.ACTIVE))
        || match.Participants.Any(participant => (!activeOnly || participant.Status == ParticipantStatus.CONFIRMED)
            && (participant.UserId == actorId || (participant.Team != null && participant.Team.Members.Any(member => member.UserId == actorId && member.Status == MemberStatus.ACTIVE)))));
    public Task<bool> CanViewMatchAsync(Guid matchId, Guid actorId, bool activeOnly, CancellationToken cancellationToken) =>
        VisibleMatches(actorId, activeOnly).AnyAsync(match => match.Id == matchId, cancellationToken);

    private IQueryable<Listing> AvailableListings(DateTimeOffset now) => db.Listings.Where(listing => listing.Status == ListingStatus.OPEN
        && listing.CutoffAt > now && listing.StartAt > now && listing.AcceptedSlots < listing.OpenSlots
        && listing.Sport.IsActive && listing.Format.IsActive
        && (listing.HostTeam == null ? listing.OwnerUser.Status == UserStatus.ACTIVE
            : listing.HostTeam.Status == TeamStatus.ACTIVE && listing.HostTeam.CaptainUser.Status == UserStatus.ACTIVE));
    private IQueryable<Listing> EligiblePlayers(Guid userId, DateTimeOffset now) => AvailableListings(now).Where(listing =>
        listing.Mode == ListingMode.FIND_PLAYERS && listing.OwnerUserId != userId
        && db.Users.Any(user => user.Id == userId && user.Status == UserStatus.ACTIVE)
        && (listing.HostTeam == null || !listing.HostTeam.Members.Any(member => member.UserId == userId && member.Status == MemberStatus.ACTIVE))
        && !listing.Requests.Any(request => request.ApplicantUserId == userId && (request.Status == RequestStatus.PENDING || request.Status == RequestStatus.ACCEPTED)));
    private static IQueryable<Listing> ApplyCriteria(IQueryable<Listing> listings, ListingCriteria criteria)
    {
        criteria = criteria.Normalize();
        if (criteria.SportId is { } sportId) listings = listings.Where(item => item.SportId == sportId);
        if (criteria.FormatId is { } formatId) listings = listings.Where(item => item.FormatId == formatId);
        if (criteria.Mode is { } mode) listings = listings.Where(item => item.Mode == mode);
        if (criteria.Level is { } level) listings = level switch
        {
            SportLevel.BEGINNER => listings.Where(item => item.LevelMin == SportLevel.BEGINNER),
            SportLevel.INTERMEDIATE => listings.Where(item => item.LevelMin != SportLevel.ADVANCED && item.LevelMax != SportLevel.BEGINNER),
            SportLevel.ADVANCED => listings.Where(item => item.LevelMax == SportLevel.ADVANCED),
            _ => throw new ArgumentOutOfRangeException(nameof(criteria))
        };
        if (criteria.StartFrom is { } from) listings = listings.Where(item => item.StartAt >= from.ToUniversalTime());
        if (criteria.StartTo is { } to) listings = listings.Where(item => item.StartAt <= to.ToUniversalTime());
        if (criteria.Latitude is { } latitude && criteria.Longitude is { } longitude && criteria.RadiusMeters is { } radius)
        {
            var point = GeoPoint(latitude, longitude);
            listings = listings.Where(item => EF.Property<Point>(item, "LocationGeography").IsWithinDistance(point, radius));
        }
        return listings;
    }
    private static Point GeoPoint(decimal latitude, decimal longitude) => new((double)decimal.Round(longitude, 6), (double)decimal.Round(latitude, 6)) { SRID = 4326 };
    public async Task<PaginatedResult<ListingSearchResult>> SearchAsync(GetListingsQuery query, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var listings = query.MineOnly ? db.Listings.Where(listing => listing.HostTeamId == null ? listing.OwnerUserId == query.ActorUserId
            : listing.HostTeam!.Members.Any(member => member.UserId == query.ActorUserId && member.Status == MemberStatus.ACTIVE)) : AvailableListings(now);
        if (query.Status is { } status) listings = listings.Where(item => item.Status == status);
        listings = ApplyCriteria(listings, query.GetCriteria());
        if (!string.IsNullOrWhiteSpace(query.SearchString)) listings = listings.Where(item => EF.Functions.ILike(item.Title, $"%{query.SearchString.Trim()}%")
            || EF.Functions.ILike(item.AddressText, $"%{query.SearchString.Trim()}%"));
        listings = Dates(listings, query);
        var total = await listings.LongCountAsync(cancellationToken);
        var point = query.Latitude is { } latitude && query.Longitude is { } longitude ? GeoPoint(latitude, longitude) : null;
        var ordered = point is null ? listings.OrderBy(item => item.StartAt).ThenBy(item => item.Id)
            : listings.OrderBy(item => EF.Property<Point>(item, "LocationGeography").Distance(point)).ThenBy(item => item.StartAt).ThenBy(item => item.Id);
        var items = await ordered.AsNoTracking().Include(item => item.Match)
            .Skip(checked((query.PageNumber - 1) * query.PageSize)).Take(query.PageSize)
            .Select(item => new ListingSearchResult(item, point == null ? null : EF.Property<Point>(item, "LocationGeography").Distance(point)))
            .ToListAsync(cancellationToken);
        return new(items, query.PageNumber, query.PageSize, total);
    }
    public async Task<PaginatedResult<ListingRequest>> ListRequestsAsync(GetListingRequestsQuery query, CancellationToken cancellationToken)
    {
        var requests = db.ListingRequests.AsNoTracking().Where(item => query.ListingId != null ? item.ListingId == query.ListingId
            : item.ApplicantUserId == query.ActorUserId || (item.ApplicantTeam != null && item.ApplicantTeam.Members.Any(member =>
                member.UserId == query.ActorUserId && member.Status == MemberStatus.ACTIVE && (member.Role == TeamRole.CAPTAIN || member.Role == TeamRole.DEPUTY))));
        if (query.Status is { } status) requests = requests.Where(item => item.Status == status);
        if (!string.IsNullOrWhiteSpace(query.SearchString)) requests = requests.Where(item =>
            EF.Functions.ILike(item.Listing.Title, $"%{query.SearchString.Trim()}%") || (item.Message != null && EF.Functions.ILike(item.Message, $"%{query.SearchString.Trim()}%")));
        return await PageAsync(Dates(requests, query).OrderByDescending(item => item.CreatedAt).ThenBy(item => item.Id), query, cancellationToken);
    }
    public async Task<PaginatedResult<Match>> ListMatchesAsync(GetMatchesQuery query, CancellationToken cancellationToken)
    {
        var matches = VisibleMatches(query.ActorUserId).AsNoTracking();
        if (query.TeamId is { } teamId) matches = matches.Where(item => item.Participants.Any(participant => participant.TeamId == teamId));
        if (query.Status is { } status) matches = matches.Where(item => item.Status == status);
        if (query.StartFrom is { } from) matches = matches.Where(item => item.StartAt >= from.ToUniversalTime());
        if (query.StartTo is { } to) matches = matches.Where(item => item.StartAt <= to.ToUniversalTime());
        if (!string.IsNullOrWhiteSpace(query.SearchString)) matches = matches.Where(item => EF.Functions.ILike(item.Listing.Title, $"%{query.SearchString.Trim()}%")
            || EF.Functions.ILike(item.AddressText, $"%{query.SearchString.Trim()}%"));
        return await PageAsync(Dates(matches, query).Include(item => item.Participants).OrderBy(item => item.StartAt).ThenBy(item => item.Id), query, cancellationToken);
    }
    public async Task<PaginatedResult<ListingWatch>> ListWatchesAsync(GetListingWatchesQuery query, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var watches = db.ListingWatches.AsNoTracking().Where(item => item.UserId == query.ActorUserId);
        if (query.SportId is { } sportId) watches = watches.Where(item => item.SportId == sportId);
        if (query.Status is { } status) watches = status == ListingWatchStatus.EXPIRED
            ? watches.Where(item => item.Status == ListingWatchStatus.EXPIRED || ((item.Status == ListingWatchStatus.ACTIVE || item.Status == ListingWatchStatus.PAUSED) && item.StartTo <= now))
            : watches.Where(item => item.Status == status && (status == ListingWatchStatus.CANCELLED || item.StartTo > now));
        if (!string.IsNullOrWhiteSpace(query.SearchString)) watches = watches.Where(item => EF.Functions.ILike(item.Sport.Name, $"%{query.SearchString.Trim()}%")
            || (item.Format != null && EF.Functions.ILike(item.Format.DisplayName, $"%{query.SearchString.Trim()}%")));
        return await PageAsync(Dates(watches, query).OrderByDescending(item => item.CreatedAt).ThenBy(item => item.Id), query, cancellationToken);
    }
    public async Task<IReadOnlyList<MatchContactDto>> GetContactsAsync(Guid matchId, CancellationToken cancellationToken)
    {
        var participants = db.MatchParticipants.Where(item => item.MatchId == matchId && item.Status == ParticipantStatus.CONFIRMED);
        var userIds = participants.Where(item => item.UserId != null).Select(item => item.UserId!.Value)
            .Concat(participants.Where(item => item.TeamId != null).Select(item => item.Team!.CaptainUserId));
        return await db.UserProfiles.Where(profile => userIds.Contains(profile.UserId) && profile.User.Status == UserStatus.ACTIVE)
            .OrderBy(profile => profile.UserId).Select(profile => new MatchContactDto(profile.UserId, profile.DisplayName, profile.ContactNote))
            .ToListAsync(cancellationToken);
    }
    private static IQueryable<T> Dates<T>(IQueryable<T> items, BaseQuery query) where T : AuditableEntity
    {
        if (query.FromDate is { } from) items = items.Where(item => item.UpdatedAt >= from.ToUniversalTime());
        if (query.ToDate is { } to) items = items.Where(item => item.UpdatedAt <= to.ToUniversalTime());
        return items;
    }
    private static async Task<PaginatedResult<T>> PageAsync<T>(IQueryable<T> items, BaseQuery query, CancellationToken cancellationToken)
    {
        var total = await items.LongCountAsync(cancellationToken);
        var page = await items.Skip(checked((query.PageNumber - 1) * query.PageSize)).Take(query.PageSize).ToListAsync(cancellationToken);
        return new(page, query.PageNumber, query.PageSize, total);
    }
    private sealed class MatchingTransaction(IDbContextTransaction transaction) : IMatchingTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken) => transaction.CommitAsync(cancellationToken);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
