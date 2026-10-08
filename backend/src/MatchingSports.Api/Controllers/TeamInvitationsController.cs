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

[Route("api/v1/teams/{teamId:guid}/invitations")]
public sealed class TeamInvitationsController(IMediator mediator, IMapper mapper, ICurrentUser currentUser) : ApiControllerBase
{
    [HttpGet, Authorize(Policy = PermissionCodes.ListTeams)]
    [ProducesResponseType<PaginatedResponse<TeamInvitationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<TeamInvitationDto>>> GetForTeam(Guid teamId, [FromQuery] GetTeamInvitationsRequest request, CancellationToken cancellationToken)
    {
        var query = mapper.Map<GetTeamInvitationsQuery>(request) with { ActorUserId = currentUser.UserId, TeamId = teamId };
        return OkPaginatedResponse(await mediator.Send(query, cancellationToken), item => item);
    }
    [HttpGet("/api/v1/team-invitations"), Authorize(Policy = PermissionCodes.ListTeams)]
    [ProducesResponseType<PaginatedResponse<TeamInvitationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<TeamInvitationDto>>> GetOwn([FromQuery] GetTeamInvitationsRequest request, CancellationToken cancellationToken)
    {
        var query = mapper.Map<GetTeamInvitationsQuery>(request) with { ActorUserId = currentUser.UserId };
        return OkPaginatedResponse(await mediator.Send(query, cancellationToken), item => item);
    }
    [HttpPost, Authorize(Policy = PermissionCodes.AddTeamMembers)]
    [ProducesResponseType<BaseResponse<TeamInvitationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<TeamInvitationDto>>> Create(Guid teamId, [FromBody] TeamUserRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new InviteTeamMemberCommand(currentUser.UserId, teamId, request.UserId), cancellationToken));
    [HttpPost("{invitationId:guid}/cancel"), Authorize(Policy = PermissionCodes.AddTeamMembers)]
    [ProducesResponseType<BaseResponse<TeamInvitationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<TeamInvitationDto>>> Cancel(Guid teamId, Guid invitationId, [FromBody] TeamVersionRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new CancelTeamInvitationCommand(currentUser.UserId, teamId, invitationId, request.ConcurrencyToken), cancellationToken));
    [HttpPost("{invitationId:guid}/accept"), Authorize(Policy = PermissionCodes.RespondTeamInvitations)]
    [ProducesResponseType<BaseResponse<TeamInvitationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<TeamInvitationDto>>> Accept(Guid teamId, Guid invitationId, [FromBody] TeamVersionRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new AcceptTeamInvitationCommand(currentUser.UserId, teamId, invitationId, request.ConcurrencyToken), cancellationToken));
    [HttpPost("{invitationId:guid}/reject"), Authorize(Policy = PermissionCodes.RespondTeamInvitations)]
    [ProducesResponseType<BaseResponse<TeamInvitationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<TeamInvitationDto>>> Reject(Guid teamId, Guid invitationId, [FromBody] TeamVersionRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new RejectTeamInvitationCommand(currentUser.UserId, teamId, invitationId, request.ConcurrencyToken), cancellationToken));
}
