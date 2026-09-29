using MatchingSports.Application.Abstractions;
using MediatR;

namespace MatchingSports.Application.Features.Identity.ResendVerification;

public sealed record ResendVerificationCommand(string Email, string? IpAddress) : IRequest;

public sealed class ResendVerificationCommandHandler(IIdentityService identityService)
    : IRequestHandler<ResendVerificationCommand>
{
    public async Task Handle(ResendVerificationCommand request, CancellationToken cancellationToken) =>
        await identityService.ResendVerificationOtpAsync(request.Email, request.IpAddress, cancellationToken);
}
