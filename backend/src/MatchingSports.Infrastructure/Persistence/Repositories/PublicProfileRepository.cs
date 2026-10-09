using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Features.PublicProfile;
using MatchingSports.Domain.Enums;
using MatchingSports.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace MatchingSports.Infrastructure.Persistence.Repositories;

internal sealed class PublicProfileRepository(MatchingSportsDbContext db) : IPublicProfileRepository
{
    public Task<PublicUserProfileDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Users.AsNoTracking().Where(PublicProfileVisibility.Users).Where(user => user.Id == userId)
            .Select(user => new PublicUserProfileDto(user.Id, user.Profile!.DisplayName, user.Profile.AvatarUrl,
                user.Profile.DefaultArea, user.SportProfiles.Where(profile => profile.Sport.IsActive)
                    .OrderBy(profile => profile.Sport.Code).ThenBy(profile => profile.Id)
                    .Select(profile => new PublicSportProfileDto(
                        new PublicSportSummaryDto(profile.SportId, profile.Sport.Code, profile.Sport.Name),
                        profile.Level, profile.Bio, profile.PreferredFormats.Where(format => format.IsActive)
                            .OrderBy(format => format.Code).ThenBy(format => format.Id)
                            .Select(format => new PublicFormatDto(format.Id, format.Code, format.DisplayName, format.SideSize)).ToList()))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<PublicTeamProfileDto?> GetTeamAsync(Guid teamId, CancellationToken cancellationToken) =>
        db.Teams.AsNoTracking().Where(PublicProfileVisibility.Teams).Where(team => team.Id == teamId)
            .Select(team => new PublicTeamProfileDto(team.Id, team.Name, team.Description, team.AvatarUrl, team.HomeArea,
                new PublicSportSummaryDto(team.SportId, team.Sport.Code, team.Sport.Name),
                new PublicUserSummaryDto(team.CaptainUserId, team.CaptainUser.Profile!.DisplayName, team.CaptainUser.Profile.AvatarUrl),
                team.Members.Count(member => member.Status == MemberStatus.ACTIVE
                    && member.User.DeletedAt == null && member.User.Status == UserStatus.ACTIVE)))
            .SingleOrDefaultAsync(cancellationToken);
}
