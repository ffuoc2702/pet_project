using MatchingSports.Application.Abstractions;
using MediatR;

namespace MatchingSports.Application.Features.Identity;

public sealed record GetCurrentUserQuery() : IRequest<CurrentUserResult>;

public sealed class GetCurrentUserQueryHandler(ICurrentUser currentUser, IIdentityService identityService)
    : IRequestHandler<GetCurrentUserQuery, CurrentUserResult>
{
    public Task<CurrentUserResult> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken) =>
        identityService.GetCurrentUserAsync(currentUser.UserId, cancellationToken);
}
