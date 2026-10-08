using AutoMapper;
using MatchingSports.Api.Contracts;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Features.Notification;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MatchingSports.Api.Controllers;

[Route("api/v1/notification-configs")]
[Authorize]
public sealed class NotificationConfigsController(IMediator mediator, ICurrentUser currentUser, IMapper mapper) : ApiControllerBase
{
    [HttpPost]
    [Authorize(Policy = PermissionCodes.CreateNotificationConfigs)]
    [ProducesResponseType<BaseResponse<NotificationConfigDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<NotificationConfigDto>>> CreateNotificationConfig(NotificationConfigRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new CreateNotificationConfigCommand(currentUser.UserId, mapper.Map<NotificationInput>(request)), cancellationToken));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionCodes.UpdateNotificationConfigs)]
    [ProducesResponseType<BaseResponse<NotificationConfigDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<NotificationConfigDto>>> UpdateNotificationConfig(Guid id, UpdateNotificationConfigRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new UpdateNotificationConfigCommand(currentUser.UserId, id, request.ConcurrencyToken, mapper.Map<NotificationInput>(request)), cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = PermissionCodes.ActivateNotificationConfigs)]
    [ProducesResponseType<BaseResponse<NotificationConfigDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<NotificationConfigDto>>> ActivateNotificationConfig(Guid id, NotificationVersionRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new ActivateNotificationConfigCommand(currentUser.UserId, id, request.ConcurrencyToken), cancellationToken));

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = PermissionCodes.CancelNotificationConfigs)]
    [ProducesResponseType<BaseResponse<NotificationConfigDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<NotificationConfigDto>>> CancelNotificationConfig(Guid id, NotificationVersionRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new CancelNotificationConfigCommand(currentUser.UserId, id, request.ConcurrencyToken), cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCodes.ReadNotificationConfigs)]
    [ProducesResponseType<BaseResponse<NotificationConfigDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<NotificationConfigDto>>> GetNotificationConfig(Guid id, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new GetNotificationConfigQuery(currentUser.UserId, id), cancellationToken));

    [HttpGet]
    [Authorize(Policy = PermissionCodes.ListNotificationConfigs)]
    [ProducesResponseType<PaginatedResponse<NotificationConfigDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<NotificationConfigDto>>> GetNotificationConfigs([FromQuery] GetNotificationConfigsRequest request, CancellationToken cancellationToken)
    {
        var query = mapper.Map<GetNotificationConfigsQuery>(request) with { ActorUserId = currentUser.UserId };
        return OkPaginatedResponse(await mediator.Send(query, cancellationToken), item => item);
    }

    [HttpPost("preview")]
    [Authorize(Policy = PermissionCodes.ReadNotificationConfigs)]
    [ProducesResponseType<BaseResponse<NotificationPreviewDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<NotificationPreviewDto>>> PreviewNotificationConfig(NotificationConfigRequest request,
        [FromQuery] NotificationPreviewPaginationRequest pagination, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new PreviewNotificationConfigQuery(currentUser.UserId, mapper.Map<NotificationInput>(request))
        { PageNumber = pagination.PageNumber, PageSize = pagination.PageSize }, cancellationToken));

    [HttpGet("{id:guid}/history")]
    [Authorize(Policy = PermissionCodes.ListNotificationHistory)]
    [ProducesResponseType<PaginatedResponse<NotificationHistoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<NotificationHistoryDto>>> GetNotificationHistory(Guid id,
        [FromQuery] GetNotificationHistoryRequest request, CancellationToken cancellationToken)
    {
        var query = mapper.Map<GetNotificationHistoryQuery>(request) with
        { ActorUserId = currentUser.UserId, NotificationConfigId = id };
        return OkPaginatedResponse(await mediator.Send(query, cancellationToken), item => item);
    }
}
