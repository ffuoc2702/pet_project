using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Identity;

public sealed record GetCurrentUserQuery() : IRequest<CurrentUserResult>, IPermissionAuthorizedRequest
{
    public Guid ActorUserId { get; init; }
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ReadOwnProfile];
}

public sealed class GetCurrentUserQueryHandler(ICurrentUser currentUser, IIdentityService identityService,
    IUserSportProfileRepository sportProfiles)
    : IRequestHandler<GetCurrentUserQuery, CurrentUserResult>
{
    public async Task<CurrentUserResult> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (request.ActorUserId != userId)
            throw new ForbiddenException(ErrorCodes.Forbidden, "Bạn chỉ được xem hồ sơ của mình.");
        var result = await identityService.GetCurrentUserAsync(userId, cancellationToken);
        return result with { SportProfiles = await sportProfiles.GetForUserAsync(userId, cancellationToken) };
    }
}
