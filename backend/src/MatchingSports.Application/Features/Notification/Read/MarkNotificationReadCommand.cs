using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Notification;

public sealed record MarkNotificationReadCommand(Guid ActorUserId, Guid Id) : IRequest<NotificationHistoryDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.MarkNotificationsRead];
}
public sealed class MarkNotificationReadCommandHandler(INotificationRepository repository, INotificationWorkRepository workRepository,
    IUnitOfWork unitOfWork, TimeProvider clock, IMapper mapper)
    : IRequestHandler<MarkNotificationReadCommand, NotificationHistoryDto>
{
    public async Task<NotificationHistoryDto> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await workRepository.BeginAsync(cancellationToken);
        var history = await repository.GetOwnHistoryAsync(request.Id, request.ActorUserId, true, clock.GetUtcNow(), cancellationToken)
            ?? throw new NotFoundException(ErrorCodes.NotificationNotFound, "Không tìm thấy thông báo của bạn.");
        history.MarkRead(clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return mapper.Map<NotificationHistoryDto>(history);
    }
}
