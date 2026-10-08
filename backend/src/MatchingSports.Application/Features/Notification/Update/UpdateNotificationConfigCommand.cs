using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Enums;
using MatchingSports.Domain.Services;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Notification;

public sealed record UpdateNotificationConfigCommand(Guid ActorUserId, Guid Id, Guid ConcurrencyToken, NotificationInput Values)
    : IRequest<NotificationConfigDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.UpdateNotificationConfigs];
}

public sealed class UpdateNotificationConfigCommandHandler(INotificationRepository repository, INotificationContentRenderer renderer,
    INotificationSchedule schedule, INotificationEmailSender emailSender, TimeProvider clock,
    IAdminActionRepository audit, IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<UpdateNotificationConfigCommand, NotificationConfigDto>
{
    public async Task<NotificationConfigDto> Handle(UpdateNotificationConfigCommand request, CancellationToken cancellationToken)
    {
        var config = await repository.GetConfigAsync(request.Id, true, cancellationToken)
            ?? throw new NotFoundException(ErrorCodes.NotificationConfigNotFound, "Không tìm thấy cấu hình thông báo.");
        NotificationRules.RequireVersion(config, request.ConcurrencyToken);
        var values = NotificationRules.Validate(() => NotificationRules.Prepare(request.Values, renderer, clock.GetUtcNow()));
        await repository.ValidateTargetsAsync(values, cancellationToken);
        var before = NotificationRules.Snapshot(config);
        if (config.Status == NotificationConfigStatus.ACTIVE) NotificationRules.RequireChannels(values, emailSender);
        NotificationRules.Validate(() => { config.Update(values, schedule, clock.GetUtcNow()); return true; });
        NotificationRules.Record(audit, request.ActorUserId, AdminActionType.UPDATE_NOTIFICATION_CONFIG, config, before);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.Map<NotificationConfigDto>(config);
    }
}
