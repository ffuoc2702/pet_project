using MatchingSports.Application.Features.UserSportProfile;
using MatchingSports.Domain.Entities;

namespace MatchingSports.Application.Abstractions;

public interface IUserSportProfileRepository
{
    Task<UserSportProfile?> GetForUpdateAsync(Guid id, Guid userId, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid userId, Guid sportId, CancellationToken cancellationToken);
    Task<Sport?> GetSportAsync(Guid sportId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<UserSportProfileDto>> GetForUserAsync(Guid userId, CancellationToken cancellationToken);
    void Add(UserSportProfile profile);
    void Update(UserSportProfile profile);
    void Remove(UserSportProfile profile);
}
