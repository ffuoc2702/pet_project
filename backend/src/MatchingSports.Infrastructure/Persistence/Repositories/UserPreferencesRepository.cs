using MatchingSports.Application.Abstractions;
using MatchingSports.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MatchingSports.Infrastructure.Persistence.Repositories;

internal sealed class UserPreferencesRepository(MatchingSportsDbContext db) : IUserPreferencesRepository
{
    public Task<UserPreferences?> GetAsync(Guid userId, bool tracked, CancellationToken cancellationToken) =>
        (tracked ? db.UserPreferences : db.UserPreferences.AsNoTracking())
            .SingleOrDefaultAsync(preferences => preferences.UserId == userId, cancellationToken);

    public void Add(UserPreferences preferences) => db.UserPreferences.Add(preferences);
}
