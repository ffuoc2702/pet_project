using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Enums;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Notification;

public sealed record GetNotificationConfigsQuery : BaseQuery<NotificationConfigDto>, IPermissionAuthorizedRequest
{
    public Guid ActorUserId { get; init; }
    public NotificationConfigStatus? Status { get; init; }
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ListNotificationConfigs];
}
public sealed class GetNotificationConfigsQueryHandler(INotificationRepository repository, IMapper mapper)
    : IRequestHandler<GetNotificationConfigsQuery, PaginatedResult<NotificationConfigDto>>
{
    public async Task<PaginatedResult<NotificationConfigDto>> Handle(GetNotificationConfigsQuery request, CancellationToken cancellationToken) =>
        (await repository.ListConfigsAsync(request, cancellationToken)).Map(mapper.Map<NotificationConfigDto>);
}

public sealed record GetNotificationConfigQuery(Guid ActorUserId, Guid Id)
    : IRequest<NotificationConfigDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ReadNotificationConfigs];
}
public sealed class GetNotificationConfigQueryHandler(INotificationRepository repository, IMapper mapper)
    : IRequestHandler<GetNotificationConfigQuery, NotificationConfigDto>
{
    public async Task<NotificationConfigDto> Handle(GetNotificationConfigQuery request, CancellationToken cancellationToken) =>
        mapper.Map<NotificationConfigDto>(await repository.GetConfigAsync(request.Id, false, cancellationToken)
            ?? throw new NotFoundException(ErrorCodes.NotificationConfigNotFound, "Không tìm thấy cấu hình thông báo."));
}

public abstract record NotificationHistoryQuery : BaseQuery<NotificationHistoryDto>
{
    public Guid ActorUserId { get; init; }
    public abstract bool AdminView { get; }
    public Guid? NotificationConfigId { get; init; }
    public bool UnreadOnly { get; init; }
    public DateTimeOffset VisibleAt { get; init; }
}
public sealed record GetNotificationHistoryQuery : NotificationHistoryQuery, IPermissionAuthorizedRequest
{
    public override bool AdminView => true;
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ListNotificationHistory];
}
public sealed record GetNotificationsQuery : NotificationHistoryQuery, IPermissionAuthorizedRequest
{
    public override bool AdminView => false;
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ListNotifications];
}
public sealed class GetNotificationHistoryQueryHandler(INotificationRepository repository, TimeProvider clock, IMapper mapper)
    : IRequestHandler<GetNotificationHistoryQuery, PaginatedResult<NotificationHistoryDto>>
{
    public async Task<PaginatedResult<NotificationHistoryDto>> Handle(GetNotificationHistoryQuery request, CancellationToken cancellationToken) =>
        (await repository.ListHistoryAsync(request with { VisibleAt = clock.GetUtcNow() }, cancellationToken)).Map(mapper.Map<NotificationHistoryDto>);
}
public sealed class GetNotificationsQueryHandler(INotificationRepository repository, TimeProvider clock, IMapper mapper)
    : IRequestHandler<GetNotificationsQuery, PaginatedResult<NotificationHistoryDto>>
{
    public async Task<PaginatedResult<NotificationHistoryDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken) =>
        (await repository.ListHistoryAsync(request with { VisibleAt = clock.GetUtcNow() }, cancellationToken)).Map(mapper.Map<NotificationHistoryDto>);
}

public sealed record GetOwnNotificationQuery(Guid ActorUserId, Guid Id) : IRequest<NotificationHistoryDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ReadNotifications];
}
public sealed class GetOwnNotificationQueryHandler(INotificationRepository repository, TimeProvider clock, IMapper mapper)
    : IRequestHandler<GetOwnNotificationQuery, NotificationHistoryDto>
{
    public async Task<NotificationHistoryDto> Handle(GetOwnNotificationQuery request, CancellationToken cancellationToken) =>
        mapper.Map<NotificationHistoryDto>(await repository.GetOwnHistoryAsync(request.Id, request.ActorUserId, false, clock.GetUtcNow(), cancellationToken)
            ?? throw new NotFoundException(ErrorCodes.NotificationNotFound, "Không tìm thấy thông báo của bạn."));
}
public sealed record GetUnreadNotificationCountQuery(Guid ActorUserId) : IRequest<UnreadNotificationCountDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ListNotifications];
}
public sealed class GetUnreadNotificationCountQueryHandler(INotificationRepository repository, TimeProvider clock)
    : IRequestHandler<GetUnreadNotificationCountQuery, UnreadNotificationCountDto>
{
    public async Task<UnreadNotificationCountDto> Handle(GetUnreadNotificationCountQuery request, CancellationToken cancellationToken) =>
        new(await repository.CountUnreadAsync(request.ActorUserId, clock.GetUtcNow(), cancellationToken));
}
