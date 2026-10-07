using MatchingSports.Api.Contracts;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Features.SystemSettings;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MatchingSports.Api.Controllers;

[Route("api/v1/system-settings")]
[Authorize]
public sealed class SystemSettingsController(IMediator mediator, ICurrentUser currentUser) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCodes.ReadSystemSettings)]
    [ProducesResponseType<BaseResponse<SystemSettingsDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<SystemSettingsDto>>> GetSystemSettings(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSystemSettingsQuery(currentUser.UserId), cancellationToken);
        return OkResponse(result);
    }

    [HttpPut]
    [Authorize(Policy = PermissionCodes.UpdateSystemSettings)]
    [ProducesResponseType<BaseResponse<SystemSettingsDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<SystemSettingsDto>>> UpdateSystemSettings(
        UpdateSystemSettingsRequest request, CancellationToken cancellationToken)
    {
        var values = new SystemSettingsInput(request.OutboxMaxRetryCount!.Value, request.MaxCaptainTeamsPerUser!.Value,
            request.MaxMemberTeamsPerUser!.Value, request.MaxDeputiesPerTeam!.Value, request.MaxMembersPerTeam!.Value,
            request.TeamInvitationLifetimeHours!.Value, request.MaxPendingTeamRequests!.Value,
            request.OutboxRetryInitialDelaySeconds!.Value, request.OutboxRetryMaxDelaySeconds!.Value,
            request.ProcessedOutboxRetentionDays!.Value, request.NotificationRetentionDays!.Value);
        var result = await mediator.Send(new UpdateSystemSettingsCommand(currentUser.UserId,
            request.ConcurrencyToken!.Value, values), cancellationToken);
        return OkResponse(result);
    }
}
