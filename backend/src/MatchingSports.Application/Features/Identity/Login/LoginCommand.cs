using MatchingSports.Application.Abstractions;
using MediatR;

namespace MatchingSports.Application.Features.Identity;

public sealed record LoginCommand(
    string Email,
    string Password,
    string? IpAddress,
    string? UserAgent) : IRequest<AuthTokenResult>;

public sealed class LoginCommandHandler(IIdentityService identityService)
    : IRequestHandler<LoginCommand, AuthTokenResult>
{
    public Task<AuthTokenResult> Handle(LoginCommand request, CancellationToken cancellationToken) =>
        identityService.LoginAsync(
            request.Email,
            request.Password,
            request.IpAddress,
            request.UserAgent,
            cancellationToken);
}
