using MatchingSports.Domain.Entities;

namespace MatchingSports.Application.Abstractions;

public interface IUserPreferencesRepository
{
    Task<UserPreferences?> GetAsync(Guid userId, bool tracked, CancellationToken cancellationToken);
    void Add(UserPreferences preferences);
}
