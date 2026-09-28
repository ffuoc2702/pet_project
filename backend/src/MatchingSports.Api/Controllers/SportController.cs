using AutoMapper;
using MatchingSports.Api.Contracts.Common;
using MatchingSports.Api.Contracts.Sport;
using MatchingSports.Application.Features.Sport.GetSport;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MatchingSports.Api.Controllers;

[Route("api/v1/sport")]
public sealed class SportController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public SportController(
        IMediator mediator,
        IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    [HttpGet]
    [ProducesResponseType<PaginatedResponse<SportResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<SportResponse>>> GetSports(
        [FromQuery] GetSportsRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSportQuery
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            ActiveOnly = request.ActiveOnly,
            SportCode = request.SportCode,
            SearchString = request.SearchString,
            FromDate = request.FromDate,
            ToDate = request.ToDate
        };

        var result = await _mediator.Send(query, cancellationToken);
        return OkPaginatedResponse(result, _mapper.Map<SportResponse>);
    }
}
