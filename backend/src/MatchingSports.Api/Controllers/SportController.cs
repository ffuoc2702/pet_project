using AutoMapper;
using MatchingSports.Api.Contracts;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Sport;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MatchingSports.Api.Controllers;

[Route("api/v1/sports")]
public sealed class SportController : ApiControllerBase
{
    private readonly ICurrentUser _currentUser;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public SportController(
        IMediator mediator,
        IMapper mapper,
        ICurrentUser currentUser)
    {
        _currentUser = currentUser;
        _mediator = mediator;
        _mapper = mapper;
    }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.ListSports)]
    [ProducesResponseType<PaginatedResponse<SportResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<SportResponse>>> GetSports(
        [FromQuery] GetSportsRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _mapper.Map<GetSportQuery>(request) with
        {
            ActorUserId = _currentUser.UserId
        };

        var result = await _mediator.Send(query, cancellationToken);
        return OkPaginatedResponse(result, _mapper.Map<SportResponse>);
    }

    [HttpGet("{sportId:guid}")]
    [Authorize(Policy = PermissionCodes.ReadSports)]
    [ProducesResponseType<BaseResponse<SportResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<SportResponse>>> GetSport(Guid sportId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSportByIdQuery(_currentUser.UserId, sportId), cancellationToken);
        return OkResponse(_mapper.Map<SportResponse>(result));
    }

}
