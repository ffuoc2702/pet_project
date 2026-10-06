using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Sport;

public sealed record GetSportByIdQuery(Guid ActorUserId, Guid SportId) : IRequest<SportDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ReadSports];
}

public sealed class GetSportByIdQueryHandler(ISportRepository sportRepository, IMapper mapper)
    : IRequestHandler<GetSportByIdQuery, SportDto>
{
    public async Task<SportDto> Handle(GetSportByIdQuery request, CancellationToken cancellationToken)
    {
        var sport = await sportRepository.GetByIdAsync(request.SportId, cancellationToken)
            ?? throw new NotFoundException(ErrorCodes.SportNotFound, "Không tìm thấy môn thể thao.");
        return mapper.Map<SportDto>(sport);
    }
}
