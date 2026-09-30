using MatchingSports.Application.Abstractions;
using MediatR;

namespace MatchingSports.Application.Features.Identity;

public sealed record ResetPasswordCommand(string Email, string Otp, string NewPassword) : IRequest;

public sealed class ResetPasswordCommandHandler(IIdentityService identityService)
    : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken) =>
        await identityService.ResetPasswordAsync(
            request.Email,
            request.Otp,
            request.NewPassword,
            cancellationToken);
}
