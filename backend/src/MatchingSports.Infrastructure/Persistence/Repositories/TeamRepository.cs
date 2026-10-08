using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Team;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace MatchingSports.Infrastructure.Persistence.Repositories;

internal sealed class TeamRepository(MatchingSportsDbContext db) : ITeamRepository
{
    // Serializable protects cross-team user quotas as well as the last available place in a team.
    public async Task<ITeamTransaction> BeginWriteAsync(CancellationToken cancellationToken) =>
        new TeamTransaction(await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken));
    public void Add(Team team) => db.Teams.Add(team);
    public void TrackNewChildren(Team team)
    {
        var autoDetect = db.ChangeTracker.AutoDetectChangesEnabled;
        try
        {
            db.ChangeTracker.AutoDetectChangesEnabled = false;
            foreach (var member in team.Members.Where(x => db.Entry(x).State == EntityState.Detached)) db.TeamMembers.Add(member);
            foreach (var invitation in team.Invitations.Where(x => db.Entry(x).State == EntityState.Detached)) db.TeamInvitations.Add(invitation);
            foreach (var request in team.JoinRequests.Where(x => db.Entry(x).State == EntityState.Detached)) db.TeamJoinRequests.Add(request);
        }
        finally { db.ChangeTracker.AutoDetectChangesEnabled = autoDetect; }
    }
    public Task<Team?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken, Guid? invitationId = null, Guid? joinRequestId = null) =>
        db.Teams.IgnoreQueryFilters().Where(x => x.DeletedAt == null && x.CaptainUser.DeletedAt == null)
            .Include(x => x.Members)
            .Include(x => x.Invitations.Where(i => i.Status == TeamRequestStatus.PENDING || i.Id == invitationId))
            .Include(x => x.JoinRequests.Where(r => r.Status == TeamRequestStatus.PENDING || r.Id == joinRequestId))
            .AsSplitQuery().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Teams.AsNoTracking().Include(x => x.Members).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public Task<bool> IsActiveUserAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(x => x.Id == id && x.Status == UserStatus.ACTIVE, cancellationToken);
    public Task<bool> IsActiveSportAsync(Guid id, CancellationToken cancellationToken) =>
        db.Sports.AnyAsync(x => x.Id == id && x.IsActive, cancellationToken);
    public Task<int> CountCaptainTeamsAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Teams.CountAsync(x => x.CaptainUserId == userId && x.Status != TeamStatus.ARCHIVED, cancellationToken);
    public Task<int> CountMemberTeamsAsync(Guid userId, CancellationToken cancellationToken) =>
        db.TeamMembers.CountAsync(x => x.UserId == userId && x.Status == MemberStatus.ACTIVE
            && x.Team.CaptainUserId != userId && x.Team.Status != TeamStatus.ARCHIVED, cancellationToken);
    public async Task<bool> HasOpenCommitmentsAsync(Guid teamId, CancellationToken cancellationToken) =>
        await db.Listings.AnyAsync(x => x.HostTeamId == teamId && x.Status != ListingStatus.COMPLETED
            && x.Status != ListingStatus.CANCELLED && x.Status != ListingStatus.EXPIRED, cancellationToken)
        || await db.MatchParticipants.AnyAsync(x => x.TeamId == teamId && x.Status == ParticipantStatus.CONFIRMED
            && x.Match.Status != MatchStatus.COMPLETED && x.Match.Status != MatchStatus.CANCELLED, cancellationToken);

    public async Task<PaginatedResult<Team>> ListAsync(GetTeamsQuery query, CancellationToken cancellationToken)
    {
        var teams = db.Teams.AsNoTracking().Where(x => query.MineOnly
            ? x.CaptainUserId == query.ActorUserId || x.Members.Any(m => m.UserId == query.ActorUserId && m.Status == MemberStatus.ACTIVE)
            : x.Status == TeamStatus.ACTIVE);
        if (query.SportId is { } sportId) teams = teams.Where(x => x.SportId == sportId);
        if (!string.IsNullOrWhiteSpace(query.SearchString)) teams = teams.Where(x => EF.Functions.ILike(x.Name, $"%{query.SearchString.Trim()}%"));
        teams = Dates(teams, query);
        return await PageAsync(teams.OrderByDescending(x => x.UpdatedAt).ThenBy(x => x.Id), query, cancellationToken);
    }
    public Task<PaginatedResult<TeamMember>> ListMembersAsync(GetTeamMembersQuery query, CancellationToken cancellationToken)
    {
        var members = db.TeamMembers.AsNoTracking().Where(x => x.TeamId == query.TeamId && x.Status == MemberStatus.ACTIVE);
        if (!string.IsNullOrWhiteSpace(query.SearchString)) members = members.Where(x =>
            x.User.Profile != null && EF.Functions.ILike(x.User.Profile.DisplayName, $"%{query.SearchString.Trim()}%"));
        return PageAsync(Dates(members, query).OrderBy(x => x.UserId), query, cancellationToken);
    }
    public async Task<PaginatedResult<TeamInvitation>> ListInvitationsAsync(GetTeamInvitationsQuery query, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var invitations = db.TeamInvitations.AsNoTracking().Where(x => query.TeamId.HasValue ? x.TeamId == query.TeamId : x.UserId == query.ActorUserId);
        if (query.Status is { } status)
            invitations = status == TeamRequestStatus.EXPIRED
                ? invitations.Where(x => x.Status == status || (x.Status == TeamRequestStatus.PENDING && x.ExpiresAt <= now))
                : invitations.Where(x => x.Status == status && (status != TeamRequestStatus.PENDING || x.ExpiresAt > now));
        if (!string.IsNullOrWhiteSpace(query.SearchString)) invitations = invitations.Where(x => EF.Functions.ILike(x.Team.Name, $"%{query.SearchString.Trim()}%"));
        var page = await PageAsync(Dates(invitations, query).OrderByDescending(x => x.UpdatedAt).ThenBy(x => x.Id), query, cancellationToken);
        foreach (var invitation in page.Items) invitation.Expire(now);
        return page;
    }
    public Task<PaginatedResult<TeamJoinRequest>> ListJoinRequestsAsync(GetTeamJoinRequestsQuery query, CancellationToken cancellationToken)
    {
        var requests = db.TeamJoinRequests.AsNoTracking().Where(x => query.TeamId.HasValue ? x.TeamId == query.TeamId : x.UserId == query.ActorUserId);
        if (query.Status is { } status) requests = requests.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(query.SearchString)) requests = requests.Where(x => EF.Functions.ILike(x.Team.Name, $"%{query.SearchString.Trim()}%"));
        return PageAsync(Dates(requests, query).OrderByDescending(x => x.UpdatedAt).ThenBy(x => x.Id), query, cancellationToken);
    }
    private static IQueryable<T> Dates<T>(IQueryable<T> source, BaseQuery query) where T : AuditableEntity
    {
        if (query.FromDate is { } from) source = source.Where(x => x.UpdatedAt >= from);
        if (query.ToDate is { } to) source = source.Where(x => x.UpdatedAt <= to);
        return source;
    }
    private static async Task<PaginatedResult<T>> PageAsync<T>(IOrderedQueryable<T> source, BaseQuery query, CancellationToken cancellationToken)
    {
        var count = await source.LongCountAsync(cancellationToken);
        var items = await source.Skip(checked((query.PageNumber - 1) * query.PageSize)).Take(query.PageSize).ToListAsync(cancellationToken);
        return new(items, query.PageNumber, query.PageSize, count);
    }
    private sealed class TeamTransaction(IDbContextTransaction transaction) : ITeamTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken) => transaction.CommitAsync(cancellationToken);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
