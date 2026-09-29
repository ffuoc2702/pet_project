using MatchingSports.Application.Abstractions;
using MediatR;

namespace MatchingSports.Application.Features.Identity.Refresh;

public sealed record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress,
    string? UserAgent) : IRequest<AuthTokenResult>;

public sealed class RefreshTokenCommandHandler(IIdentityService identityService)
    : IRequestHandler<RefreshTokenCommand, AuthTokenResult>
{
    public Task<AuthTokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken) =>
        identityService.RefreshAsync(
            request.RefreshToken,
            request.IpAddress,
            request.UserAgent,
            cancellationToken);
}
