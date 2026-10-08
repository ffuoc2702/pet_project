using MatchingSports.Domain.Enums;
using MatchingSports.Shared;

namespace MatchingSports.Domain.Entities;

public sealed class NotificationHistory : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? NotificationConfigId { get; private set; }
    public Guid? TeamId { get; private set; }
    public int SendNumber { get; private set; }
    public DateTimeOffset ScheduledAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public NotificationCritical Critical { get; private set; }
    public string BodyText { get; private set; } = string.Empty;
    public bool InAppEnabled { get; private set; } = true;
    public NotificationDeliveryStatus EmailStatus { get; private set; }
    public NotificationDeliveryStatus PushStatus { get; private set; }
    public NotificationDeliveryStatus SmsStatus { get; private set; }
    public DateTimeOffset? EmailSentAt { get; private set; }
    public DateTimeOffset? PushSentAt { get; private set; }
    public DateTimeOffset? SmsSentAt { get; private set; }
    public string? EmailErrorCode { get; private set; }
    public string? PushErrorCode { get; private set; }
    public string? SmsErrorCode { get; private set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    /// <summary>Entity identifier from <see cref="EntityTypes"/>.</summary>
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset? ReadAt { get; set; }
    public DateTimeOffset PurgeAfter { get; private set; }

    public User? User { get; set; }

    public static NotificationHistory Create(NotificationConfig config, Guid userId, IReadOnlyCollection<Guid> sourceTeamIds)
    {
        if (userId == Guid.Empty || config.SchedulingStatus != NotificationSchedulingStatus.SCHEDULING)
            throw new ArgumentException("Người nhận hoặc lượt gửi không hợp lệ.");
        var metadata = System.Text.Json.Nodes.JsonNode.Parse(config.Metadata)!.AsObject();
        metadata["SourceTeamIds"] = System.Text.Json.JsonSerializer.SerializeToNode(sourceTeamIds);
        return new NotificationHistory
        {
            Id = Guid.NewGuid(),
            NotificationConfigId = config.Id,
            UserId = userId,
            TeamId = sourceTeamIds.Count == 1 ? sourceTeamIds.Single() : null,
            SendNumber = checked(config.SendCount + 1),
            ScheduledAt = config.NextRunAt!.Value,
            ExpiresAt = config.ExpiredAfter,
            Critical = config.Critical,
            Title = config.Title,
            Body = config.Body,
            BodyText = config.BodyText,
            PayloadJson = metadata.ToJsonString(),
            EntityType = metadata[nameof(EntityType)]?.GetValue<string>(),
            EntityId = Guid.TryParse(metadata[nameof(EntityId)]?.GetValue<string>(), out var entityId) ? entityId : null,
            Type = NotificationType.ADMIN_MESSAGE,
            InAppEnabled = config.InAppEnabled,
            EmailStatus = config.EmailEnabled ? NotificationDeliveryStatus.PENDING : NotificationDeliveryStatus.DISABLED,
            PushStatus = config.PushEnabled ? NotificationDeliveryStatus.PENDING : NotificationDeliveryStatus.DISABLED,
            SmsStatus = config.SmsEnabled ? NotificationDeliveryStatus.PENDING : NotificationDeliveryStatus.DISABLED
        };
    }

    public void MarkRead(DateTimeOffset now)
    {
        if (!InAppEnabled) throw new InvalidOperationException("Thông báo không được hiển thị trong app.");
        ReadAt ??= now;
    }

    public void RecordDelivery(NotificationChannel channel, NotificationDeliveryStatus status, DateTimeOffset now, string? errorCode = null)
    {
        if (!Enum.IsDefined(channel) || status is not (NotificationDeliveryStatus.SENT or NotificationDeliveryStatus.FAILED or NotificationDeliveryStatus.SKIPPED))
            throw new ArgumentException("Kết quả gửi không hợp lệ.");
        switch (channel)
        {
            case NotificationChannel.EMAIL:
                if (EmailStatus != NotificationDeliveryStatus.PENDING) return;
                EmailStatus = status; EmailSentAt = status == NotificationDeliveryStatus.SENT ? now : null; EmailErrorCode = errorCode; break;
            case NotificationChannel.PUSH:
                if (PushStatus != NotificationDeliveryStatus.PENDING) return;
                PushStatus = status; PushSentAt = status == NotificationDeliveryStatus.SENT ? now : null; PushErrorCode = errorCode; break;
            case NotificationChannel.SMS:
                if (SmsStatus != NotificationDeliveryStatus.PENDING) return;
                SmsStatus = status; SmsSentAt = status == NotificationDeliveryStatus.SENT ? now : null; SmsErrorCode = errorCode; break;
        }
    }

    public void InitializeRetention(SystemSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (CreatedAt == default)
            throw new InvalidOperationException("Phải xác định thời điểm tạo thông báo trước khi tính thời điểm dọn dữ liệu.");
        if (NotificationConfigId is null && string.IsNullOrEmpty(BodyText) && !string.IsNullOrEmpty(Body))
        {
            // Existing event notifications use plain text; preserve it for mobile and encode the HTML view.
            BodyText = Body;
            Body = System.Net.WebUtility.HtmlEncode(Body);
            var payload = System.Text.Json.Nodes.JsonNode.Parse(PayloadJson);
            var metadata = payload as System.Text.Json.Nodes.JsonObject
                ?? new System.Text.Json.Nodes.JsonObject { ["LegacyPayload"] = payload };
            metadata["SchemaVersion"] = 1;
            PayloadJson = metadata.ToJsonString();
        }
        PurgeAfter = settings.GetNotificationPurgeAfter(CreatedAt);
    }
}
