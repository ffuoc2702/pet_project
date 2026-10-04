using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Shared;
using MediatR;
namespace MatchingSports.Application.Features.Sport;

public sealed record GetSportQuery : BaseQuery<SportDto>, IPermissionAuthorizedRequest
{
    public Guid ActorUserId { get; init; }
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ListSports];
    public bool ActiveOnly { get; init; } = true;
    public string? SportCode { get; init; }
}

public sealed class GetSportQueryHandler(ISportRepository sportRepository, IMapper mapper)
    : IRequestHandler<GetSportQuery, PaginatedResult<SportDto>>
{
    public async Task<PaginatedResult<SportDto>> Handle(
        GetSportQuery request,
        CancellationToken cancellationToken)
    {
        var sports = await sportRepository.ListAsync(request, cancellationToken);
        return sports.Map(sport => mapper.Map<SportDto>(sport));
    }
}
