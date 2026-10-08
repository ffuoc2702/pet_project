using AutoMapper;
using MatchingSports.Api.Contracts;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Team;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MatchingSports.Api.Controllers;

[Route("api/v1/teams/{teamId:guid}/join-requests")]
public sealed class TeamJoinRequestsController(IMediator mediator, IMapper mapper, ICurrentUser currentUser) : ApiControllerBase
{
    [HttpGet, Authorize(Policy = PermissionCodes.ListTeams)]
    [ProducesResponseType<PaginatedResponse<TeamJoinRequestDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<TeamJoinRequestDto>>> GetForTeam(Guid teamId, [FromQuery] GetTeamJoinRequestsRequest request, CancellationToken cancellationToken)
    {
        var query = mapper.Map<GetTeamJoinRequestsQuery>(request) with { ActorUserId = currentUser.UserId, TeamId = teamId };
        return OkPaginatedResponse(await mediator.Send(query, cancellationToken), item => item);
    }
    [HttpGet("/api/v1/team-join-requests"), Authorize(Policy = PermissionCodes.ListTeams)]
    [ProducesResponseType<PaginatedResponse<TeamJoinRequestDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<TeamJoinRequestDto>>> GetOwn([FromQuery] GetTeamJoinRequestsRequest request, CancellationToken cancellationToken)
    {
        var query = mapper.Map<GetTeamJoinRequestsQuery>(request) with { ActorUserId = currentUser.UserId };
        return OkPaginatedResponse(await mediator.Send(query, cancellationToken), item => item);
    }
    [HttpPost, Authorize(Policy = PermissionCodes.CreateTeamJoinRequests)]
    [ProducesResponseType<BaseResponse<TeamJoinRequestDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<TeamJoinRequestDto>>> Create(Guid teamId, [FromBody] CreateTeamJoinRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new CreateTeamJoinRequestCommand(currentUser.UserId, teamId, request.Message), cancellationToken));
    [HttpPost("{joinRequestId:guid}/withdraw"), Authorize(Policy = PermissionCodes.WithdrawTeamJoinRequests)]
    [ProducesResponseType<BaseResponse<TeamJoinRequestDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<TeamJoinRequestDto>>> Withdraw(Guid teamId, Guid joinRequestId, [FromBody] TeamVersionRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new WithdrawTeamJoinRequestCommand(currentUser.UserId, teamId, joinRequestId, request.ConcurrencyToken), cancellationToken));
    [HttpPost("{joinRequestId:guid}/accept"), Authorize(Policy = PermissionCodes.AddTeamMembers)]
    [ProducesResponseType<BaseResponse<TeamJoinRequestDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<TeamJoinRequestDto>>> Accept(Guid teamId, Guid joinRequestId, [FromBody] TeamVersionRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new AcceptTeamJoinRequestCommand(currentUser.UserId, teamId, joinRequestId, request.ConcurrencyToken), cancellationToken));
    [HttpPost("{joinRequestId:guid}/reject"), Authorize(Policy = PermissionCodes.AddTeamMembers)]
    [ProducesResponseType<BaseResponse<TeamJoinRequestDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<TeamJoinRequestDto>>> Reject(Guid teamId, Guid joinRequestId, [FromBody] TeamVersionRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new RejectTeamJoinRequestCommand(currentUser.UserId, teamId, joinRequestId, request.ConcurrencyToken), cancellationToken));
}
