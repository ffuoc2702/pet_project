using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Domain.Services;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Notification;

public sealed record CreateNotificationConfigCommand(Guid ActorUserId, NotificationInput Values)
    : IRequest<NotificationConfigDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.CreateNotificationConfigs];
}

public sealed class CreateNotificationConfigCommandHandler(INotificationRepository repository, INotificationContentRenderer renderer,
    INotificationSchedule schedule, TimeProvider clock, IAdminActionRepository audit, IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<CreateNotificationConfigCommand, NotificationConfigDto>
{
    public async Task<NotificationConfigDto> Handle(CreateNotificationConfigCommand request, CancellationToken cancellationToken)
    {
        var config = NotificationRules.Validate(() => NotificationConfig.Create(NotificationRules.Prepare(request.Values, renderer, clock.GetUtcNow()), schedule));
        await repository.ValidateTargetsAsync(config.GetValues(), cancellationToken);
        repository.Add(config);
        NotificationRules.Record(audit, request.ActorUserId, AdminActionType.CREATE_NOTIFICATION_CONFIG, config, null);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.Map<NotificationConfigDto>(config);
    }
}
