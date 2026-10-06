using AutoMapper;
using AutoMapper.QueryableExtensions;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Features.UserSportProfile;
using MatchingSports.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MatchingSports.Infrastructure.Persistence.Repositories;

internal sealed class UserSportProfileRepository(MatchingSportsDbContext db, IMapper mapper) : IUserSportProfileRepository
{
    public Task<UserSportProfile?> GetForUpdateAsync(Guid id, Guid userId, CancellationToken cancellationToken) =>
        db.UserSportProfiles.Include(profile => profile.Sport)
                                .ThenInclude(sport => sport.Formats)
                            .Include(profile => profile.PreferredFormats)
            .SingleOrDefaultAsync(profile => profile.Id == id && profile.UserId == userId, cancellationToken);

    public Task<bool> ExistsAsync(Guid userId, Guid sportId, CancellationToken cancellationToken) =>
        db.UserSportProfiles.AnyAsync(profile => profile.UserId == userId && profile.SportId == sportId, cancellationToken);

    public Task<Sport?> GetSportAsync(Guid sportId, CancellationToken cancellationToken) =>
        db.Sports.Include(sport => sport.Formats).SingleOrDefaultAsync(sport => sport.Id == sportId, cancellationToken);

    public async Task<IReadOnlyCollection<UserSportProfileDto>> GetForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.UserSportProfiles.AsNoTracking().Where(profile => profile.UserId == userId)
            .OrderBy(profile => profile.Sport.Name).ThenBy(profile => profile.Id)
            .ProjectTo<UserSportProfileDto>(mapper.ConfigurationProvider).ToListAsync(cancellationToken);

    public void Add(UserSportProfile profile) => db.UserSportProfiles.Add(profile);
    public void Update(UserSportProfile profile) => db.Entry(profile).Property(item => item.UpdatedAt).IsModified = true;
    public void Remove(UserSportProfile profile) => db.UserSportProfiles.Remove(profile);
}
