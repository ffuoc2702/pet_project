using MatchingSports.Application.Abstractions;
using MediatR;

namespace MatchingSports.Application.Features.Identity.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string DisplayName,
    string? IpAddress) : IRequest<RegistrationResult>;

public sealed class RegisterCommandHandler(IIdentityService identityService)
    : IRequestHandler<RegisterCommand, RegistrationResult>
{
    public Task<RegistrationResult> Handle(RegisterCommand request, CancellationToken cancellationToken) =>
        identityService.RegisterAsync(
            request.Email,
            request.Password,
            request.DisplayName,
            request.IpAddress,
            cancellationToken);
}
