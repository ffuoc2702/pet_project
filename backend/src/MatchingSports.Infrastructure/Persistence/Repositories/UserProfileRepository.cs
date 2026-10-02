using MatchingSports.Application.Abstractions;
using MatchingSports.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MatchingSports.Infrastructure.Persistence.Repositories;

internal sealed class UserProfileRepository(MatchingSportsDbContext db)
    : IUserProfileRepository
{
    public Task<UserProfile?> GetForUpdateAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        db.UserProfiles.SingleOrDefaultAsync(
            profile => profile.UserId == userId,
            cancellationToken);
}
