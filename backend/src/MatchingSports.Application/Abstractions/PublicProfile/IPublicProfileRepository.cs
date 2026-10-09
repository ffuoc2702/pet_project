using MatchingSports.Application.Features.PublicProfile;

namespace MatchingSports.Application.Abstractions;

public interface IPublicProfileRepository
{
    Task<PublicUserProfileDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<PublicTeamProfileDto?> GetTeamAsync(Guid teamId, CancellationToken cancellationToken);
}
