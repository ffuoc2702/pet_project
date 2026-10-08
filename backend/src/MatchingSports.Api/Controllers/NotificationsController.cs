using AutoMapper;
using MatchingSports.Api.Contracts;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Features.Notification;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MatchingSports.Api.Controllers;

[Route("api/v1/notifications")]
[Authorize]
public sealed class NotificationsController(IMediator mediator, ICurrentUser currentUser, IMapper mapper) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCodes.ListNotifications)]
    [ProducesResponseType<PaginatedResponse<NotificationHistoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<NotificationHistoryDto>>> GetNotifications([FromQuery] GetNotificationHistoryRequest request, CancellationToken cancellationToken)
    {
        var query = mapper.Map<GetNotificationsQuery>(request) with { ActorUserId = currentUser.UserId };
        return OkPaginatedResponse(await mediator.Send(query, cancellationToken), item => item);
    }
    [HttpGet("unread-count")]
    [Authorize(Policy = PermissionCodes.ListNotifications)]
    [ProducesResponseType<BaseResponse<UnreadNotificationCountDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<UnreadNotificationCountDto>>> GetUnreadCount(CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new GetUnreadNotificationCountQuery(currentUser.UserId), cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCodes.ReadNotifications)]
    [ProducesResponseType<BaseResponse<NotificationHistoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<NotificationHistoryDto>>> GetNotification(Guid id, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new GetOwnNotificationQuery(currentUser.UserId, id), cancellationToken));

    [HttpPost("{id:guid}/read")]
    [Authorize(Policy = PermissionCodes.MarkNotificationsRead)]
    [ProducesResponseType<BaseResponse<NotificationHistoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<NotificationHistoryDto>>> MarkRead(Guid id, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new MarkNotificationReadCommand(currentUser.UserId, id), cancellationToken));
}
