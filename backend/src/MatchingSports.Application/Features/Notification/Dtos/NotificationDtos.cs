using MatchingSports.Application.Common;
using MatchingSports.Domain.Enums;

namespace MatchingSports.Application.Features.Notification;

public sealed record NotificationInput(
    bool IsSendNow, NotificationScheduleType ScheduleType, string? CronString, NotificationCritical Critical,
    string Title, string Body, DateTimeOffset AvailableAt, DateTimeOffset? ExpiredAfter, int? MaxSendTimes,
    NotificationAudienceType AudienceType, IReadOnlyCollection<Guid> UserIds, IReadOnlyCollection<Guid> TeamIds,
    bool InAppEnabled, bool EmailEnabled, bool PushEnabled, bool SmsEnabled, string Metadata, string TimeZoneId, string? Description = null);

public sealed record NotificationConfigDto : ConcurrentBaseDto
{
    public string? Description { get; init; }
    public Guid Id { get; init; }
    public bool IsSendNow { get; init; }
    public NotificationScheduleType ScheduleType { get; init; }
    public string? CronString { get; init; }
    public NotificationCritical Critical { get; init; }
    public string Title { get; init; } = null!;
    public string Body { get; init; } = null!;
    public string BodyText { get; init; } = null!;
    public DateTimeOffset AvailableAt { get; init; }
    public DateTimeOffset? ExpiredAfter { get; init; }
    public int? MaxSendTimes { get; init; }
    public NotificationAudienceType AudienceType { get; init; }
    public IReadOnlyCollection<Guid> UserIds { get; init; } = [];
    public IReadOnlyCollection<Guid> TeamIds { get; init; } = [];
    public bool InAppEnabled { get; init; }
    public bool EmailEnabled { get; init; }
    public bool PushEnabled { get; init; }
    public bool SmsEnabled { get; init; }
    public System.Text.Json.JsonElement Metadata { get; init; }
    public string TimeZoneId { get; init; } = null!;
    public NotificationConfigStatus Status { get; init; }
    public NotificationSchedulingStatus SchedulingStatus { get; init; }
    public DateTimeOffset? NextRunAt { get; init; }
    public DateTimeOffset? LastRunAt { get; init; }
    public int SendCount { get; init; }
    public DateTimeOffset? LockedUntil { get; init; }
    public string? AbortReason { get; init; }
}

public sealed record NotificationHistoryDto : BaseDto
{
    public Guid Id { get; init; }
    public Guid? NotificationConfigId { get; init; }
    public Guid? UserId { get; init; }
    public Guid? TeamId { get; init; }
    public int SendNumber { get; init; }
    public DateTimeOffset ScheduledAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public DateTimeOffset? ReadAt { get; init; }
    public DateTimeOffset PurgeAfter { get; init; }
    public NotificationType Type { get; init; }
    public NotificationCritical Critical { get; init; }
    public string Title { get; init; } = null!;
    public string Body { get; init; } = null!;
    public string BodyText { get; init; } = null!;
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public System.Text.Json.JsonElement Metadata { get; init; }
    public bool InAppEnabled { get; init; }
    public NotificationDeliveryStatus EmailStatus { get; init; }
    public NotificationDeliveryStatus PushStatus { get; init; }
    public NotificationDeliveryStatus SmsStatus { get; init; }
    public DateTimeOffset? EmailSentAt { get; init; }
    public DateTimeOffset? PushSentAt { get; init; }
    public DateTimeOffset? SmsSentAt { get; init; }
    public string? EmailErrorCode { get; init; }
    public string? PushErrorCode { get; init; }
    public string? SmsErrorCode { get; init; }
}

public sealed record NotificationPreviewDto(string Body, string BodyText, IReadOnlyCollection<DateTimeOffset> NextOccurrences,
    PaginatedResult<NotificationRecipientDto> Recipients);
public sealed record NotificationRecipientDto(Guid UserId, IReadOnlyCollection<Guid> SourceTeamIds);
public sealed record UnreadNotificationCountDto(long Count);
