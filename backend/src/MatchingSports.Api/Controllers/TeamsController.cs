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

[Route("api/v1/teams")]
public sealed class TeamsController(IMediator mediator, IMapper mapper, ICurrentUser currentUser) : ApiControllerBase
{
    [HttpGet, Authorize(Policy = PermissionCodes.ListTeams)]
    [ProducesResponseType<PaginatedResponse<TeamDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<TeamDto>>> GetTeams([FromQuery] GetTeamsRequest request, CancellationToken cancellationToken)
    {
        var query = mapper.Map<GetTeamsQuery>(request) with { ActorUserId = currentUser.UserId };
        return OkPaginatedResponse(await mediator.Send(query, cancellationToken), item => item);
    }
    [HttpGet("{teamId:guid}"), Authorize(Policy = PermissionCodes.ReadTeams)]
    [ProducesResponseType<BaseResponse<TeamDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<TeamDto>>> GetTeam(Guid teamId, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new GetTeamQuery(currentUser.UserId, teamId), cancellationToken));
    [HttpGet("{teamId:guid}/members"), Authorize(Policy = PermissionCodes.ListTeams)]
    [ProducesResponseType<PaginatedResponse<TeamMemberDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<TeamMemberDto>>> GetMembers(Guid teamId, [FromQuery] GetTeamMembersRequest request, CancellationToken cancellationToken)
    {
        var query = mapper.Map<GetTeamMembersQuery>(request) with { ActorUserId = currentUser.UserId, TeamId = teamId };
        return OkPaginatedResponse(await mediator.Send(query, cancellationToken), item => item);
    }
    [HttpPost, Authorize(Policy = PermissionCodes.CreateTeams)]
    [ProducesResponseType<BaseResponse<TeamDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<TeamDto>>> CreateTeam([FromBody] CreateTeamRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new CreateTeamCommand(currentUser.UserId, request.SportId, request.Name,
            request.Description, request.HomeArea, request.AvatarUrl), cancellationToken));
    [HttpPut("{teamId:guid}"), Authorize(Policy = PermissionCodes.UpdateTeams)]
    [ProducesResponseType<BaseResponse<TeamDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<TeamDto>>> UpdateTeam(Guid teamId, [FromBody] UpdateTeamRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new UpdateTeamCommand(currentUser.UserId, teamId, request.ConcurrencyToken,
            request.Name, request.Description, request.HomeArea, request.AvatarUrl), cancellationToken));
    [HttpDelete("{teamId:guid}"), Authorize(Policy = PermissionCodes.DeleteTeams)]
    [ProducesResponseType<BaseResponse<TeamDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<TeamDto>>> DeleteTeam(Guid teamId, [FromBody] TeamVersionRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new DeleteTeamCommand(currentUser.UserId, teamId, request.ConcurrencyToken), cancellationToken));
    [HttpPost("{teamId:guid}/captain/transfer"), Authorize(Policy = PermissionCodes.TransferTeamCaptain)]
    [ProducesResponseType<BaseResponse<TeamDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<TeamDto>>> TransferCaptain(Guid teamId, [FromBody] TransferTeamCaptainRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new TransferTeamCaptainCommand(currentUser.UserId, teamId, request.UserId, request.ConcurrencyToken), cancellationToken));
    [HttpPost("{teamId:guid}/members/{userId:guid}/deputy"), Authorize(Policy = PermissionCodes.ManageTeamDeputies)]
    [ProducesResponseType<BaseResponse<TeamDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<TeamDto>>> AppointDeputy(Guid teamId, Guid userId, [FromBody] TeamVersionRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new AppointTeamDeputyCommand(currentUser.UserId, teamId, userId, request.ConcurrencyToken), cancellationToken));
    [HttpDelete("{teamId:guid}/members/{userId:guid}/deputy"), Authorize(Policy = PermissionCodes.ManageTeamDeputies)]
    [ProducesResponseType<BaseResponse<TeamDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<TeamDto>>> RevokeDeputy(Guid teamId, Guid userId, [FromBody] TeamVersionRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new RevokeTeamDeputyCommand(currentUser.UserId, teamId, userId, request.ConcurrencyToken), cancellationToken));
    [HttpDelete("{teamId:guid}/members/{userId:guid}"), Authorize(Policy = PermissionCodes.RemoveTeamMembers)]
    [ProducesResponseType<BaseResponse<TeamDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<TeamDto>>> RemoveMember(Guid teamId, Guid userId, [FromBody] TeamVersionRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new RemoveTeamMemberCommand(currentUser.UserId, teamId, userId, request.ConcurrencyToken), cancellationToken));
    [HttpPost("{teamId:guid}/leave"), Authorize(Policy = PermissionCodes.LeaveTeams)]
    [ProducesResponseType<BaseResponse<TeamDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<TeamDto>>> LeaveTeam(Guid teamId, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new LeaveTeamCommand(currentUser.UserId, teamId), cancellationToken));
}
