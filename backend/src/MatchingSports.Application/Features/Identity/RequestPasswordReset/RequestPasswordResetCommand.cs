using MatchingSports.Application.Abstractions;
using MediatR;

namespace MatchingSports.Application.Features.Identity;

public sealed record RequestPasswordResetCommand(string Email, string? IpAddress) : IRequest;

public sealed class RequestPasswordResetCommandHandler(IIdentityService identityService)
    : IRequestHandler<RequestPasswordResetCommand>
{
    public async Task Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken) =>
        await identityService.RequestPasswordResetAsync(request.Email, request.IpAddress, cancellationToken);
}
