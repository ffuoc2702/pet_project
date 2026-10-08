using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Enums;
using MatchingSports.Domain.Services;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Notification;

public sealed record ActivateNotificationConfigCommand(Guid ActorUserId, Guid Id, Guid ConcurrencyToken)
    : IRequest<NotificationConfigDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ActivateNotificationConfigs];
}

public sealed class ActivateNotificationConfigCommandHandler(INotificationRepository repository, INotificationSchedule schedule,
    INotificationEmailSender emailSender, TimeProvider clock, IAdminActionRepository audit, IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<ActivateNotificationConfigCommand, NotificationConfigDto>
{
    public async Task<NotificationConfigDto> Handle(ActivateNotificationConfigCommand request, CancellationToken cancellationToken)
    {
        var config = await repository.GetConfigAsync(request.Id, true, cancellationToken)
            ?? throw new NotFoundException(ErrorCodes.NotificationConfigNotFound, "Không tìm thấy cấu hình thông báo.");
        NotificationRules.RequireVersion(config, request.ConcurrencyToken);
        NotificationRules.RequireChannels(config, emailSender);
        await repository.ValidateTargetsAsync(config.GetValues(), cancellationToken);
        var before = NotificationRules.Snapshot(config);
        NotificationRules.Validate(() => { config.Activate(schedule, clock.GetUtcNow()); return true; });
        NotificationRules.Record(audit, request.ActorUserId, AdminActionType.ACTIVATE_NOTIFICATION_CONFIG, config, before);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.Map<NotificationConfigDto>(config);
    }
}
