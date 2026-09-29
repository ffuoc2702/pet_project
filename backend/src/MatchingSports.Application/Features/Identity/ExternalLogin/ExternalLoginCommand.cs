using MatchingSports.Application.Abstractions;
using MediatR;

namespace MatchingSports.Application.Features.Identity.ExternalLogin;

public sealed record ExternalLoginCommand(
    string Provider,
    string ProviderKey,
    string Email,
    string DisplayName,
    string? IpAddress,
    string? UserAgent) : IRequest<AuthTokenResult>;

public sealed class ExternalLoginCommandHandler(IIdentityService identityService)
    : IRequestHandler<ExternalLoginCommand, AuthTokenResult>
{
    public Task<AuthTokenResult> Handle(ExternalLoginCommand request, CancellationToken cancellationToken) =>
        identityService.ExternalLoginAsync(
            request.Provider,
            request.ProviderKey,
            request.Email,
            request.DisplayName,
            request.IpAddress,
            request.UserAgent,
            cancellationToken);
}
