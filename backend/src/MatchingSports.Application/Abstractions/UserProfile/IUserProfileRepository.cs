using MatchingSports.Domain.Entities;

namespace MatchingSports.Application.Abstractions;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetForUpdateAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
