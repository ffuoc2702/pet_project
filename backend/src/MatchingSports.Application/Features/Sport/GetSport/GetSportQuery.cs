using AutoMapper;
using MatchingSports.Application.Abstractions.Persistence;
using MatchingSports.Application.Common.Pagination;
using MatchingSports.Application.Common.Queries;
using MediatR;

namespace MatchingSports.Application.Features.Sport.GetSport;

public sealed class GetSportQuery : BaseQuery<SportItem>
{
    public bool ActiveOnly { get; init; } = true;
    public string? SportCode { get; init; }
}

public sealed class GetSportQueryHandler
    : IRequestHandler<GetSportQuery, PaginatedResult<SportItem>>
{
    private readonly ISportRepository _repository;
    private readonly IMapper _mapper;

    public GetSportQueryHandler(
        ISportRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<SportItem>> Handle(
        GetSportQuery request,
        CancellationToken cancellationToken)
    {
        var sports = await _repository.ListAsync(request, cancellationToken);
        return sports.Map(sport => _mapper.Map<SportItem>(sport));
    }
}
