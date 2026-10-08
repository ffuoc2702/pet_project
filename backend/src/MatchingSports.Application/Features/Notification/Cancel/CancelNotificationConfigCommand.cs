using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Enums;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Notification;

public sealed record CancelNotificationConfigCommand(Guid ActorUserId, Guid Id, Guid ConcurrencyToken)
    : IRequest<NotificationConfigDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.CancelNotificationConfigs];
}

public sealed class CancelNotificationConfigCommandHandler(INotificationRepository repository, IAdminActionRepository audit,
    IUnitOfWork unitOfWork, IMapper mapper) : IRequestHandler<CancelNotificationConfigCommand, NotificationConfigDto>
{
    public async Task<NotificationConfigDto> Handle(CancelNotificationConfigCommand request, CancellationToken cancellationToken)
    {
        var config = await repository.GetConfigAsync(request.Id, true, cancellationToken)
            ?? throw new NotFoundException(ErrorCodes.NotificationConfigNotFound, "Không tìm thấy cấu hình thông báo.");
        NotificationRules.RequireVersion(config, request.ConcurrencyToken);
        var before = NotificationRules.Snapshot(config);
        config.Cancel();
        NotificationRules.Record(audit, request.ActorUserId, AdminActionType.CANCEL_NOTIFICATION_CONFIG, config, before);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.Map<NotificationConfigDto>(config);
    }
}
