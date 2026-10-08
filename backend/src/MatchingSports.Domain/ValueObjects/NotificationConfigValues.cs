using MatchingSports.Domain.Enums;
using MatchingSports.Shared;

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
        string normalizedMetadata;
        try { normalizedMetadata = NotificationMetadata.NormalizeAndValidate(Metadata); }
        catch (ArgumentException exception) { throw new ArgumentException(exception.GetValidationMessage(), nameof(Metadata), exception); }
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

}
