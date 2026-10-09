using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.PublicProfile;

public sealed record GetPublicUserProfileQuery(Guid ActorUserId, Guid UserId) : IRequest<PublicUserProfileDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ReadPublicUserProfiles];
}

public sealed record GetPublicTeamProfileQuery(Guid ActorUserId, Guid TeamId) : IRequest<PublicTeamProfileDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ReadPublicTeamProfiles];
}

public sealed class PublicProfileQueryHandlers(IPublicProfileRepository repository)
    : IRequestHandler<GetPublicUserProfileQuery, PublicUserProfileDto>,
      IRequestHandler<GetPublicTeamProfileQuery, PublicTeamProfileDto>
{
    public async Task<PublicUserProfileDto> Handle(GetPublicUserProfileQuery request, CancellationToken cancellationToken) =>
        await repository.GetUserAsync(request.UserId, cancellationToken)
        ?? throw new NotFoundException(ErrorCodes.UserProfileNotFound, "Không tìm thấy hồ sơ công khai của người dùng.");

    public async Task<PublicTeamProfileDto> Handle(GetPublicTeamProfileQuery request, CancellationToken cancellationToken) =>
        await repository.GetTeamAsync(request.TeamId, cancellationToken)
        ?? throw new NotFoundException(ErrorCodes.TeamNotFound, "Không tìm thấy hồ sơ công khai của đội.");
}
