using MatchingSports.Application.Abstractions;
using MediatR;

namespace MatchingSports.Application.Features.Identity.VerifyEmail;

public sealed record VerifyEmailCommand(string Email, string Otp) : IRequest;

public sealed class VerifyEmailCommandHandler(IIdentityService identityService)
    : IRequestHandler<VerifyEmailCommand>
{
    public async Task Handle(VerifyEmailCommand request, CancellationToken cancellationToken) =>
        await identityService.VerifyEmailAsync(request.Email, request.Otp, cancellationToken);
}
