using System.Text.Json;
using MatchingSports.Domain.Enums;

namespace MatchingSports.Domain.ValueObjects;

public sealed record NotificationConfigValues(
    bool IsSendNow, NotificationScheduleType ScheduleType, string? CronString,
    NotificationCritical Critical, string Title, string Body, string BodyText,
    DateTimeOffset AvailableAt, DateTimeOffset? ExpiredAfter, int? MaxSendTimes,
    NotificationAudienceType AudienceType, IReadOnlyCollection<Guid> UserIds, IReadOnlyCollection<Guid> TeamIds,
    bool InAppEnabled, bool EmailEnabled, bool PushEnabled, bool SmsEnabled,
    string Metadata, string TimeZoneId)
{
    public NotificationConfigValues NormalizeAndValidate()
    {
        if (!Enum.IsDefined(ScheduleType) || !Enum.IsDefined(Critical) || !Enum.IsDefined(AudienceType))
            throw new ArgumentException("Loại cấu hình thông báo không hợp lệ.");
        if (string.IsNullOrWhiteSpace(Title) || Title.Trim().Length > 160 || Title.Contains('\r') || Title.Contains('\n'))
            throw new ArgumentException("Tiêu đề phải có từ 1 đến 160 ký tự và không chứa xuống dòng.", nameof(Title));
        if (string.IsNullOrWhiteSpace(Body) || Body.Length > 100000 || string.IsNullOrWhiteSpace(BodyText))
            throw new ArgumentException("Nội dung thông báo không hợp lệ.", nameof(Body));
        if (!InAppEnabled && !EmailEnabled && !PushEnabled && !SmsEnabled)
            throw new ArgumentException("Phải bật ít nhất một kênh thông báo.");
        if (AvailableAt == default || ExpiredAfter <= AvailableAt)
            throw new ArgumentException("Hạn gửi phải sau thời điểm bắt đầu.", nameof(ExpiredAfter));
        if (MaxSendTimes is <= 0 || (ScheduleType == NotificationScheduleType.ONCE && MaxSendTimes != 1))
            throw new ArgumentException("Thông báo một lần phải có giới hạn một lượt.", nameof(MaxSendTimes));
        if (ScheduleType == NotificationScheduleType.SCHEDULED && (string.IsNullOrWhiteSpace(CronString) || CronString.Length > 120))
            throw new ArgumentException("Cron là bắt buộc với lịch lặp.", nameof(CronString));
        if (ScheduleType == NotificationScheduleType.ONCE && !string.IsNullOrWhiteSpace(CronString))
            throw new ArgumentException("Thông báo một lần không dùng cron.", nameof(CronString));
        if (string.IsNullOrWhiteSpace(TimeZoneId) || TimeZoneId.Length > 100)
            throw new ArgumentException("Múi giờ không hợp lệ.", nameof(TimeZoneId));
        ArgumentNullException.ThrowIfNull(UserIds);
        ArgumentNullException.ThrowIfNull(TeamIds);
        if (UserIds.Contains(Guid.Empty) || TeamIds.Contains(Guid.Empty) || UserIds.Count > 10000 || TeamIds.Count > 10000)
            throw new ArgumentException("Danh sách đối tượng nhận không hợp lệ.");
        if ((AudienceType == NotificationAudienceType.SELECTED_USERS && (UserIds.Count == 0 || TeamIds.Count != 0))
            || (AudienceType == NotificationAudienceType.SELECTED_TEAMS && (TeamIds.Count == 0 || UserIds.Count != 0))
            || (AudienceType == NotificationAudienceType.ALL_USERS && (UserIds.Count != 0 || TeamIds.Count != 0)))
            throw new ArgumentException("Danh sách đối tượng không khớp loại người nhận.");
        if (string.IsNullOrWhiteSpace(Metadata) || Metadata.Length > 16000)
            throw new ArgumentException("Metadata không hợp lệ.", nameof(Metadata));
        string normalizedMetadata;
        try
        {
            normalizedMetadata = NormalizeMetadata(Metadata);
            using var metadata = JsonDocument.Parse(normalizedMetadata);
            if (metadata.RootElement.ValueKind != JsonValueKind.Object
                || !metadata.RootElement.TryGetProperty("SchemaVersion", out var version)
                || !version.TryGetInt32(out var number) || number != 1)
                throw new ArgumentException("Metadata phải là object với SchemaVersion bằng 1.", nameof(Metadata));
            foreach (var name in new[] { "ActionType", "EntityType" })
                if (metadata.RootElement.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.String)
                    throw new ArgumentException("Thông tin điều hướng phải là chuỗi.", nameof(Metadata));
            if (metadata.RootElement.TryGetProperty("EntityId", out var entityId)
                && (entityId.ValueKind != JsonValueKind.String || !entityId.TryGetGuid(out var id) || id == Guid.Empty))
                throw new ArgumentException("EntityId trong metadata phải là UUID.", nameof(Metadata));
            if (metadata.RootElement.TryGetProperty("ActionType", out var action))
            {
                if (action.GetString() is not ("NONE" or "VIEW_ENTITY" or "OPEN_NOTIFICATIONS"))
                    throw new ArgumentException("ActionType không được hỗ trợ trong schema hiện tại.", nameof(Metadata));
                if (action.GetString() == "VIEW_ENTITY" && (!metadata.RootElement.TryGetProperty("EntityId", out _)
                    || !metadata.RootElement.TryGetProperty("EntityType", out var targetType) || string.IsNullOrWhiteSpace(targetType.GetString())))
                    throw new ArgumentException("Điều hướng tới đối tượng cần EntityType và EntityId.", nameof(Metadata));
            }
        }
        catch (JsonException) { throw new ArgumentException("Metadata phải là JSON hợp lệ.", nameof(Metadata)); }
        return this with
        {
            Title = Title.Trim(),
            Metadata = normalizedMetadata,
            CronString = string.IsNullOrWhiteSpace(CronString) ? null : CronString.Trim(),
            AvailableAt = AvailableAt.ToUniversalTime(),
            ExpiredAfter = ExpiredAfter?.ToUniversalTime(),
            TimeZoneId = TimeZoneId.Trim(),
            UserIds = UserIds.Distinct().ToArray(),
            TeamIds = TeamIds.Distinct().ToArray()
        };
    }

    private static string NormalizeMetadata(string value)
    {
        using var document = JsonDocument.Parse(value);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Metadata phải là JSON object.", nameof(value));
        string[] knownNames = ["SchemaVersion", "ActionType", "EntityType", "EntityId", "SourceTeamIds"];
        var properties = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            var name = knownNames.FirstOrDefault(candidate => string.Equals(candidate, property.Name, StringComparison.OrdinalIgnoreCase))
                ?? property.Name;
            if (!properties.TryAdd(name, property.Value))
                throw new ArgumentException("Metadata không được chứa khóa trùng nhau.", nameof(value));
        }
        var result = JsonSerializer.Serialize(properties);
        if (result.Length > 16000)
            throw new ArgumentException("Metadata không hợp lệ.", nameof(value));
        return result;
    }
}
