using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Notification;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Domain.ValueObjects;
using MatchingSports.Shared;
using Microsoft.EntityFrameworkCore;

namespace MatchingSports.Infrastructure.Persistence.Repositories;

internal sealed class NotificationRepository(MatchingSportsDbContext db) : INotificationRepository
{
    public void Add(NotificationConfig config) => db.NotificationConfigs.Add(config);

    public Task<NotificationConfig?> GetConfigAsync(Guid id, bool tracked, CancellationToken cancellationToken)
    {
        var query = db.NotificationConfigs.AsQueryable();
        if (!tracked) query = query.AsNoTracking();
        return query.Include(x => x.Users).Include(x => x.Teams).AsSplitQuery().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<PaginatedResult<NotificationConfig>> ListConfigsAsync(GetNotificationConfigsQuery query, CancellationToken cancellationToken)
    {
        var configs = db.NotificationConfigs.AsNoTracking();
        if (query.Status is { } status) configs = configs.Where(x => x.Status == status);
        if (query.FromDate is { } from) configs = configs.Where(x => x.UpdatedAt >= from);
        if (query.ToDate is { } to) configs = configs.Where(x => x.UpdatedAt <= to);
        if (!string.IsNullOrWhiteSpace(query.SearchString))
        {
            var search = $"%{query.SearchString.Trim()}%";
            configs = configs.Where(x => EF.Functions.ILike(x.Title, search));
        }
        var count = await configs.LongCountAsync(cancellationToken);
        var items = await configs.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)
            .Skip(checked((query.PageNumber - 1) * query.PageSize)).Take(query.PageSize)
            .Include(x => x.Users).Include(x => x.Teams).AsSplitQuery().ToListAsync(cancellationToken);
        return new(items, query.PageNumber, query.PageSize, count);
    }

    public async Task ValidateTargetsAsync(NotificationConfigValues values, CancellationToken cancellationToken)
    {
        if ((values.AudienceType == NotificationAudienceType.SELECTED_USERS
                && await EligibleUsers().LongCountAsync(x => values.UserIds.Contains(x.Id), cancellationToken) != values.UserIds.Count)
            || (values.AudienceType == NotificationAudienceType.SELECTED_TEAMS
                && await db.Teams.LongCountAsync(x => values.TeamIds.Contains(x.Id) && x.DeletedAt == null && x.Status == TeamStatus.ACTIVE, cancellationToken) != values.TeamIds.Count))
            throw new RequestValidationException(ErrorCodes.InvalidNotificationConfig, "Đối tượng nhận không tồn tại hoặc không hoạt động.",
                new Dictionary<string, string[]> { ["audience"] = ["Vui lòng kiểm tra danh sách user/team."] });
    }

    internal IQueryable<User> EligibleUsers() => db.Users.Where(x => x.Status == UserStatus.ACTIVE);

    internal IQueryable<User> AudienceUsers(NotificationConfigValues values)
    {
        var users = EligibleUsers();
        if (values.AudienceType == NotificationAudienceType.SELECTED_USERS)
            return users.Where(x => values.UserIds.Contains(x.Id));
        if (values.AudienceType == NotificationAudienceType.SELECTED_TEAMS)
        {
            var members = db.TeamMembers.Where(x => values.TeamIds.Contains(x.TeamId)
                && x.Status == MemberStatus.ACTIVE && x.Team.DeletedAt == null && x.Team.Status == TeamStatus.ACTIVE).Select(x => x.UserId);
            var captains = db.Teams.Where(x => values.TeamIds.Contains(x.Id) && x.DeletedAt == null && x.Status == TeamStatus.ACTIVE).Select(x => x.CaptainUserId);
            return users.Where(x => members.Contains(x.Id) || captains.Contains(x.Id));
        }
        return users;
    }

    internal async Task<IReadOnlyCollection<NotificationRecipient>> MaterializeRecipientsAsync(NotificationConfigValues values,
        IReadOnlyCollection<Guid>? ids, CancellationToken cancellationToken)
    {
        var users = AudienceUsers(values).AsNoTracking();
        if (ids is not null) users = users.Where(x => ids.Contains(x.Id));
        var recipients = await users.Select(x => new { x.Id, x.Email, x.EmailConfirmed }).ToListAsync(cancellationToken);
        var recipientIds = recipients.Select(x => x.Id).ToArray();
        var sources = new Dictionary<Guid, List<Guid>>();
        if (values.AudienceType == NotificationAudienceType.SELECTED_TEAMS)
        {
            var members = await db.TeamMembers.Where(x => values.TeamIds.Contains(x.TeamId) && recipientIds.Contains(x.UserId)
                && x.Status == MemberStatus.ACTIVE && x.Team.DeletedAt == null && x.Team.Status == TeamStatus.ACTIVE)
                .Select(x => new { x.UserId, x.TeamId }).ToListAsync(cancellationToken);
            var captains = await db.Teams.Where(x => values.TeamIds.Contains(x.Id) && recipientIds.Contains(x.CaptainUserId)
                && x.DeletedAt == null && x.Status == TeamStatus.ACTIVE)
                .Select(x => new { UserId = x.CaptainUserId, TeamId = x.Id }).ToListAsync(cancellationToken);
            foreach (var source in members.Concat(captains))
            {
                if (!sources.TryGetValue(source.UserId, out var teams)) sources[source.UserId] = teams = [];
                if (!teams.Contains(source.TeamId)) teams.Add(source.TeamId);
            }
        }
        return recipients.Select(x => new NotificationRecipient(x.Id, x.Email, x.EmailConfirmed,
            sources.TryGetValue(x.Id, out var teams) ? teams : [])).ToArray();
    }

    public async Task<PaginatedResult<NotificationRecipient>> PreviewRecipientsAsync(NotificationConfigValues values, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var users = AudienceUsers(values);
        var count = await users.LongCountAsync(cancellationToken);
        var ids = await users.OrderBy(x => x.Id).Skip(checked((pageNumber - 1) * pageSize)).Take(pageSize)
            .Select(x => x.Id).ToArrayAsync(cancellationToken);
        var recipients = await MaterializeRecipientsAsync(values, ids, cancellationToken);
        return new(recipients.OrderBy(x => x.UserId).ToArray(), pageNumber, pageSize, count);
    }

    private IQueryable<NotificationHistory> VisibleHistory(Guid userId, DateTimeOffset now) =>
        db.NotificationHistories.Where(x => x.UserId == userId && x.InAppEnabled && x.PurgeAfter > now);

    public async Task<PaginatedResult<NotificationHistory>> ListHistoryAsync(NotificationHistoryQuery query, CancellationToken cancellationToken)
    {
        var history = query.AdminView ? db.NotificationHistories.AsQueryable() : VisibleHistory(query.ActorUserId, query.VisibleAt);
        if (query.NotificationConfigId is { } configId) history = history.Where(x => x.NotificationConfigId == configId);
        if (query.UnreadOnly) history = history.Where(x => x.ReadAt == null);
        if (query.FromDate is { } from) history = history.Where(x => x.UpdatedAt >= from);
        if (query.ToDate is { } to) history = history.Where(x => x.UpdatedAt <= to);
        if (!string.IsNullOrWhiteSpace(query.SearchString))
        {
            var search = $"%{query.SearchString.Trim()}%";
            history = history.Where(x => EF.Functions.ILike(x.Title, search));
        }
        var count = await history.LongCountAsync(cancellationToken);
        var items = await history.AsNoTracking().OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)
            .Skip(checked((query.PageNumber - 1) * query.PageSize)).Take(query.PageSize).ToListAsync(cancellationToken);
        return new(items, query.PageNumber, query.PageSize, count);
    }

    public Task<NotificationHistory?> GetOwnHistoryAsync(Guid id, Guid userId, bool tracked, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var query = tracked
            ? db.NotificationHistories.FromSqlInterpolated($"SELECT * FROM notification_history WHERE id = {id} AND user_id = {userId} FOR UPDATE")
            : db.NotificationHistories.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId && x.InAppEnabled && x.PurgeAfter > now, cancellationToken);
    }

    public Task<long> CountUnreadAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken) =>
        VisibleHistory(userId, now).LongCountAsync(x => x.ReadAt == null, cancellationToken);
}
