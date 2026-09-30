using AutoMapper;
using MatchingSports.Application.Common.Pagination;
using MatchingSports.Application.Common.Queries;
using MatchingSports.Application.Abstractions;
using MediatR;
using MatchingSports.Application.Features.Sport.Dtos;
namespace MatchingSports.Application.Features.Sport;

public sealed class GetSportQuery : BaseQuery<SportDto>
{
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
