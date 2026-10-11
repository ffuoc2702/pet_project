using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Shared;
using MediatR;
namespace MatchingSports.Application.Features.Sport;

public sealed record GetSportsQuery : BaseQuery<SportDto>, IPermissionAuthorizedRequest
{
    public Guid ActorUserId { get; init; }
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ListSports];
    public bool? IsActive { get; init; }
    public string? SportCode { get; init; }
}

public sealed class GetSportsQueryHandler(ISportRepository sportRepository, IMapper mapper)
    : IRequestHandler<GetSportsQuery, PaginatedResult<SportDto>>
{
    public async Task<PaginatedResult<SportDto>> Handle(
        GetSportsQuery request,
        CancellationToken cancellationToken)
    {
        var sports = await sportRepository.ListAsync(request, cancellationToken);
        return sports.Map(sport => mapper.Map<SportDto>(sport));
    }
}
