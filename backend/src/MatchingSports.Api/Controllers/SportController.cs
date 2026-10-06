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
public sealed class SportController(IMediator mediator, IMapper mapper, ICurrentUser currentUser) : ApiControllerBase
{

    [HttpGet]
    [Authorize(Policy = PermissionCodes.ListSports)]
    [ProducesResponseType<PaginatedResponse<SportDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<SportDto>>> GetSports(
        [FromQuery] GetSportsRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = mapper.Map<GetSportsQuery>(request) with
        {
            ActorUserId = currentUser.UserId
        };

        var result = await mediator.Send(query, cancellationToken);
        return OkPaginatedResponse(result, item => item);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.CreateSports)]
    [ProducesResponseType<BaseResponse<SportDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<SportDto>>> CreateSport(
        [FromBody] CreateSportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateSportCommand(
            currentUser.UserId, request.Code, request.Name, mapper.Map<List<SportFormatInput>>(request.Formats)), cancellationToken);
        return OkResponse(result);
    }

    [HttpPut("{sportId:guid}")]
    [Authorize(Policy = PermissionCodes.UpdateSports)]
    [ProducesResponseType<BaseResponse<SportDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<SportDto>>> UpdateSport(
        Guid sportId,
        [FromBody] UpdateSportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateSportCommand(
            currentUser.UserId, sportId, request.Code, request.Name, mapper.Map<List<SportFormatInput>>(request.Formats)), cancellationToken);
        return OkResponse(result);
    }

    [HttpDelete("{sportId:guid}")]
    [Authorize(Policy = PermissionCodes.DeactivateSports)]
    [ProducesResponseType<BaseResponse<SportDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<SportDto>>> DeactivateSport(
        Guid sportId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeactivateSportCommand(
            currentUser.UserId, sportId), cancellationToken);
        return OkResponse(result);
    }

    [HttpGet("{sportId:guid}")]
    [Authorize(Policy = PermissionCodes.ReadSports)]
    [ProducesResponseType<BaseResponse<SportDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<SportDto>>> GetSport(Guid sportId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSportByIdQuery(currentUser.UserId, sportId), cancellationToken);
        return OkResponse(result);
    }

}
