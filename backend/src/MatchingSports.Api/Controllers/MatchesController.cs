using AutoMapper;
using MatchingSports.Api.Contracts;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Features.Matching;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MatchingSports.Api.Controllers;

[Route("api/v1/matches")]
[Authorize]
public sealed class MatchesController(IMediator mediator, ICurrentUser currentUser, IMapper mapper) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCodes.ListMatches)]
    [ProducesResponseType<PaginatedResponse<MatchDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<MatchDto>>> GetMatches([FromQuery] GetMatchesRequest request, CancellationToken cancellationToken)
    {
        var query = mapper.Map<GetMatchesQuery>(request) with { ActorUserId = currentUser.UserId };
        return OkPaginatedResponse(await mediator.Send(query, cancellationToken), item => item);
    }
    [HttpGet("{matchId:guid}")]
    [Authorize(Policy = PermissionCodes.ReadMatches)]
    [ProducesResponseType<BaseResponse<MatchDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<MatchDto>>> GetMatch(Guid matchId, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new GetMatchQuery(currentUser.UserId, matchId), cancellationToken));
    [HttpGet("{matchId:guid}/contact")]
    [Authorize(Policy = PermissionCodes.ReadMatchContact)]
    [ProducesResponseType<BaseResponse<MatchContactsDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<MatchContactsDto>>> GetContacts(Guid matchId, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new GetMatchContactsQuery(currentUser.UserId, matchId), cancellationToken));
    [HttpPost("{matchId:guid}/confirm")]
    [Authorize(Policy = PermissionCodes.ConfirmMatches)]
    [ProducesResponseType<BaseResponse<MatchDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<MatchDto>>> ConfirmMatch(Guid matchId, [FromBody] MatchingVersionRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new ConfirmMatchCommand(currentUser.UserId, matchId, request.ConcurrencyToken!.Value), cancellationToken));
    [HttpPost("{matchId:guid}/cancel")]
    [Authorize(Policy = PermissionCodes.CancelMatches)]
    [ProducesResponseType<BaseResponse<MatchDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<MatchDto>>> CancelMatch(Guid matchId, [FromBody] MatchingReasonRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new CancelMatchCommand(currentUser.UserId, matchId, request.ConcurrencyToken!.Value, request.Reason), cancellationToken));
    [HttpPost("/api/v1/admin/matches/{matchId:guid}/cancel")]
    [Authorize(Policy = PermissionCodes.CancelAnyMatch)]
    [ProducesResponseType<BaseResponse<MatchDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<MatchDto>>> CancelAnyMatch(Guid matchId, [FromBody] MatchingReasonRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new CancelAnyMatchCommand(currentUser.UserId, matchId, request.ConcurrencyToken!.Value, request.Reason), cancellationToken));
    [HttpPost("{matchId:guid}/complete")]
    [Authorize(Policy = PermissionCodes.CompleteMatches)]
    [ProducesResponseType<BaseResponse<MatchDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<MatchDto>>> CompleteMatch(Guid matchId, [FromBody] MatchingVersionRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new CompleteMatchCommand(currentUser.UserId, matchId, request.ConcurrencyToken!.Value), cancellationToken));
}
