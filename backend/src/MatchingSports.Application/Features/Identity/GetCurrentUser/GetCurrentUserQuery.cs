using MatchingSports.Application.Abstractions;
using MediatR;

namespace MatchingSports.Application.Features.Identity;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<CurrentUserResult>;

public sealed class GetCurrentUserQueryHandler(IIdentityService identityService)
    : IRequestHandler<GetCurrentUserQuery, CurrentUserResult>
{
    public Task<CurrentUserResult> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken) =>
        identityService.GetCurrentUserAsync(request.UserId, cancellationToken);
}
