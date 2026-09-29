using AutoMapper;
using MatchingSports.Application.Common.Pagination;
using MatchingSports.Application.Common.Queries;
using MatchingSports.Application.Abstractions;
using MediatR;
namespace MatchingSports.Application.Features.Sport.GetSport;

public sealed class GetSportQuery : BaseQuery<SportItem>
{
    public bool ActiveOnly { get; init; } = true;
    public string? SportCode { get; init; }
}

public sealed class GetSportQueryHandler(ISportRepository sportRepository, IMapper mapper)
    : IRequestHandler<GetSportQuery, PaginatedResult<SportItem>>
{
    public async Task<PaginatedResult<SportItem>> Handle(
        GetSportQuery request,
        CancellationToken cancellationToken)
    {
        var sports = await sportRepository.ListAsync(request, cancellationToken);
        return sports.Map(sport => mapper.Map<SportItem>(sport));
    }
}
