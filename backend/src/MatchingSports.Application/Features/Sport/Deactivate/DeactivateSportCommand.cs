using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Sport;

public sealed record DeactivateSportCommand(Guid ActorUserId, Guid SportId)
    : IRequest<SportDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.DeactivateSports];
}

public sealed class DeactivateSportCommandHandler(ISportRepository sports, IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<DeactivateSportCommand, SportDto>
{
    public async Task<SportDto> Handle(DeactivateSportCommand request, CancellationToken cancellationToken)
    {
        var sport = await sports.GetForUpdateAsync(request.SportId, cancellationToken)
            ?? throw new NotFoundException(ErrorCodes.SportNotFound, "Không tìm thấy môn thể thao.");

        sport.Deactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.Map<SportDto>(sport);
    }
}
