using MatchingSports.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MatchingSports.DemoSimulator;

internal static class DemoReportBuilder
{
    public static async Task<DemoReport> BuildAsync(DemoSession session, DemoState state, bool testDatabase)
    {
        var userIds = Enumerable.Range(0, state.UserCount).Select(i => state.Resources[$"user-{i}"]).ToArray();
        var teamIds = Enumerable.Range(0, state.TeamCount).Select(i => state.Resources[$"team-{i}"]).ToArray();
        var listingIds = Enumerable.Range(0, state.ListingCount).Select(i => state.Resources[$"listing-{i}"]).ToArray();
        var members = await session.ReadAsync((db, ct) => db.TeamMembers.AsNoTracking()
            .Where(m => teamIds.Contains(m.TeamId) && m.Status == MemberStatus.ACTIVE)
            .Select(m => new { m.UserId, m.TeamId, m.Role }).ToListAsync(ct));
        var users = userIds.Select((id, i) => new DemoUser(i + 1, id, DemoSeeder.Email(state.RunId, i),
            members.Where(m => m.UserId == id).Select(m => (Guid?)m.TeamId).SingleOrDefault())).ToArray();
        var teams = await session.ReadAsync((db, ct) => db.Teams.AsNoTracking().Where(t => teamIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Name, t.CaptainUserId }).ToListAsync(ct));
        var teamReports = teams.Select(t => new DemoTeam(t.Id, t.Name, t.CaptainUserId,
            members.Single(m => m.TeamId == t.Id && m.Role == TeamRole.DEPUTY).UserId)).ToArray();
        var listings = await session.ReadAsync((db, ct) => db.Listings.AsNoTracking().Where(l => listingIds.Contains(l.Id))
            .OrderBy(l => l.Title).Select(l => new DemoListing(l.Id, l.Title, l.Status.ToString(), db.Matches.Where(m => m.ListingId == l.Id).Select(m => (Guid?)m.Id).SingleOrDefault(), l.StartAt)).ToListAsync(ct));
        var matchIds = listings.Where(l => l.MatchId.HasValue).Select(l => l.MatchId!.Value).ToArray();
        var notifications = await session.ReadAsync((db, ct) => db.NotificationHistories.LongCountAsync(h => h.UserId.HasValue && userIds.Contains(h.UserId.Value), ct));
        var watches = await session.ReadAsync((db, ct) => db.ListingWatchDeliveries.CountAsync(d => listingIds.Contains(d.ListingId) && userIds.Contains(d.UserId), ct));
        var reminders = await session.ReadAsync((db, ct) => db.NotificationHistories.CountAsync(h => h.UserId.HasValue && userIds.Contains(h.UserId.Value)
            && h.Type == NotificationType.MATCH_REMINDER && h.EntityId.HasValue && matchIds.Contains(h.EntityId.Value), ct));
        return new DemoReport(state.RunId, DateTimeOffset.UtcNow, testDatabase, users, teamReports, listings,
            state.CompletedSteps.Order(StringComparer.Ordinal).ToArray(), notifications, watches, reminders);
    }
}
