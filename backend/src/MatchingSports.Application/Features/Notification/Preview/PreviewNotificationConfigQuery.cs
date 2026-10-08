using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Domain.Services;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Notification;

public sealed record PreviewNotificationConfigQuery(Guid ActorUserId, NotificationInput Values) : PaginatedQuery,
    IRequest<NotificationPreviewDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ReadNotificationConfigs];
}
public sealed class PreviewNotificationConfigQueryHandler(INotificationRepository repository, INotificationContentRenderer renderer,
    INotificationSchedule schedule, TimeProvider clock) : IRequestHandler<PreviewNotificationConfigQuery, NotificationPreviewDto>
{
    public async Task<NotificationPreviewDto> Handle(PreviewNotificationConfigQuery request, CancellationToken cancellationToken)
    {
        var config = NotificationRules.Validate(() => NotificationConfig.Create(NotificationRules.Prepare(request.Values, renderer, clock.GetUtcNow()), schedule));
        await repository.ValidateTargetsAsync(config.GetValues(), cancellationToken);
        NotificationRules.Validate(() => { config.Activate(schedule, clock.GetUtcNow()); return true; });
        var occurrences = new List<DateTimeOffset> { config.NextRunAt!.Value };
        while (config.ScheduleType == NotificationScheduleType.SCHEDULED && occurrences.Count < 5
            && (config.MaxSendTimes is null || occurrences.Count < config.MaxSendTimes))
        {
            var next = schedule.GetNextOccurrence(config.CronString!, config.TimeZoneId, occurrences[^1]);
            if (next is null || next >= config.ExpiredAfter) break;
            occurrences.Add(next.Value);
        }
        var recipients = await repository.PreviewRecipientsAsync(config.GetValues(), request.PageNumber, request.PageSize, cancellationToken);
        return new(config.Body, config.BodyText, occurrences, recipients.Map(x => new NotificationRecipientDto(x.UserId, x.SourceTeamIds)));
    }
}
