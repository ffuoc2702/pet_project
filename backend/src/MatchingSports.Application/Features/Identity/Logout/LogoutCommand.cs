using MatchingSports.Application.Abstractions;
using MediatR;

namespace MatchingSports.Application.Features.Identity.Logout;

public sealed record LogoutCommand(string RefreshToken) : IRequest;

public sealed class LogoutCommandHandler(IIdentityService identityService)
    : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken) =>
        await identityService.LogoutAsync(request.RefreshToken, cancellationToken);
}
