using MatchingSports.Domain.Enums;
using MatchingSports.Domain.Services;
using MatchingSports.Domain.ValueObjects;

namespace MatchingSports.Domain.Entities;

public sealed class NotificationConfig : ConcurrentAuditableEntity
{
    public Guid Id { get; private set; }
    public bool IsSendNow { get; private set; }
    public NotificationScheduleType ScheduleType { get; private set; }
    public string? CronString { get; private set; }
    public NotificationCritical Critical { get; private set; }
    public string? Description { get; private set; }
    public string Title { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public string BodyText { get; private set; } = null!;
    public DateTimeOffset AvailableAt { get; private set; }
    public DateTimeOffset? ExpiredAfter { get; private set; }
    public int? MaxSendTimes { get; private set; }
    public NotificationAudienceType AudienceType { get; private set; }
    public bool InAppEnabled { get; private set; }
    public bool EmailEnabled { get; private set; }
    public bool PushEnabled { get; private set; }
    public bool SmsEnabled { get; private set; }
    public string Metadata { get; private set; } = null!;
    public string TimeZoneId { get; private set; } = null!;
    public NotificationConfigStatus Status { get; private set; }
    public NotificationSchedulingStatus SchedulingStatus { get; private set; }
    public DateTimeOffset? NextRunAt { get; private set; }
    public DateTimeOffset? LastRunAt { get; private set; }
    public int SendCount { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }
    public Guid? LeaseToken { get; private set; }
    public string? AbortReason { get; private set; }
    private readonly List<NotificationConfigUser> _users = [];
    private readonly List<NotificationConfigTeam> _teams = [];
    public IReadOnlyCollection<NotificationConfigUser> Users => _users.AsReadOnly();
    public IReadOnlyCollection<NotificationConfigTeam> Teams => _teams.AsReadOnly();
    private NotificationConfig() { }

    public static NotificationConfig Create(NotificationConfigValues values, INotificationSchedule schedule)
    {
        var config = new NotificationConfig { Id = Guid.NewGuid() };
        config.Apply(values.NormalizeAndValidate(), schedule);
        return config;
    }

    public void Update(NotificationConfigValues values, INotificationSchedule schedule, DateTimeOffset now)
    {
        if (Status == NotificationConfigStatus.CANCEL || SchedulingStatus is NotificationSchedulingStatus.SCHEDULING
            or NotificationSchedulingStatus.COMPLETED or NotificationSchedulingStatus.ABORTED)
            throw new InvalidOperationException("Cấu hình đã kết thúc hoặc đang được lập lịch.");
        var normalized = values.NormalizeAndValidate();
        if (normalized.MaxSendTimes is { } maximum && maximum <= SendCount)
            throw new ArgumentException("Giới hạn lượt phải lớn hơn số lượt đã tạo.", nameof(values));
        var next = Status == NotificationConfigStatus.ACTIVE ? CalculateNext(normalized, schedule, now, SendCount == 0) : null;
        if (Status == NotificationConfigStatus.ACTIVE && next is null)
            throw new ArgumentException("Lịch không còn thời điểm gửi hợp lệ.");
        Apply(normalized, schedule);
        NextRunAt = next;
        RenewConcurrencyToken();
    }

    public void Activate(INotificationSchedule schedule, DateTimeOffset now)
    {
        if (Status != NotificationConfigStatus.DRAFT)
            throw new InvalidOperationException("Chỉ được kích hoạt bản nháp.");
        var next = CalculateNext(GetValues(), schedule, now, true)
            ?? throw new ArgumentException("Lịch không còn thời điểm gửi hợp lệ.");
        Status = NotificationConfigStatus.ACTIVE;
        if (IsSendNow) AvailableAt = now;
        SchedulingStatus = NotificationSchedulingStatus.PENDING;
        NextRunAt = next;
    }

    public void Cancel()
    {
        if (Status == NotificationConfigStatus.CANCEL) return;
        Status = NotificationConfigStatus.CANCEL;
        SchedulingStatus = NotificationSchedulingStatus.ABORTED;
        AbortReason = "ADMIN_CANCELLED";
        NextRunAt = null;
        ReleaseLease();
    }

    public bool CanSchedule(DateTimeOffset now) => Status == NotificationConfigStatus.ACTIVE
        && SchedulingStatus is NotificationSchedulingStatus.PENDING or NotificationSchedulingStatus.SCHEDULING
        && NextRunAt <= now && (LockedUntil is null || LockedUntil <= now);

    public Guid Claim(DateTimeOffset now, TimeSpan leaseDuration)
    {
        if (!CanSchedule(now) || leaseDuration <= TimeSpan.Zero)
            throw new InvalidOperationException("Cấu hình không thể nhận quyền xử lý.");
        SchedulingStatus = NotificationSchedulingStatus.SCHEDULING;
        LockedUntil = now.Add(leaseDuration);
        LeaseToken = Guid.NewGuid();
        return LeaseToken.Value;
    }

    public bool OwnsLease(Guid token, DateTimeOffset now) => Status == NotificationConfigStatus.ACTIVE
        && SchedulingStatus == NotificationSchedulingStatus.SCHEDULING && LeaseToken == token && LockedUntil > now;

    public bool HasExpired(DateTimeOffset now) => ExpiredAfter <= now;

    public void CompleteExpired()
    {
        SchedulingStatus = NotificationSchedulingStatus.COMPLETED;
        NextRunAt = null;
        ReleaseLease();
    }

    public void CompleteOccurrence(INotificationSchedule schedule, DateTimeOffset now)
    {
        if (SchedulingStatus != NotificationSchedulingStatus.SCHEDULING)
            throw new InvalidOperationException("Chưa nhận quyền lập lịch.");
        var nextCount = checked(SendCount + 1);
        var next = ScheduleType == NotificationScheduleType.ONCE || MaxSendTimes <= nextCount ? null
            : schedule.GetNextOccurrence(CronString!, TimeZoneId, now);
        if (next >= ExpiredAfter) next = null;
        SendCount = nextCount;
        LastRunAt = now;
        NextRunAt = next;
        SchedulingStatus = next is null ? NotificationSchedulingStatus.COMPLETED : NotificationSchedulingStatus.PENDING;
        ReleaseLease();
    }

    public void Abort(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (reason.Length > 200) throw new ArgumentException("Lý do dừng tối đa 200 ký tự.", nameof(reason));
        AbortReason = reason;
        SchedulingStatus = NotificationSchedulingStatus.ABORTED;
        NextRunAt = null;
        ReleaseLease();
    }

    public NotificationConfigValues GetValues() => new(IsSendNow, ScheduleType, CronString, Critical, Title, Body, BodyText,
        AvailableAt, ExpiredAfter, MaxSendTimes, AudienceType, _users.Select(x => x.UserId).ToArray(),
        _teams.Select(x => x.TeamId).ToArray(), InAppEnabled, EmailEnabled, PushEnabled, SmsEnabled, Metadata, TimeZoneId, Description);

    private void Apply(NotificationConfigValues values, INotificationSchedule schedule)
    {
        // Validate provider-independent invariants before modifying the aggregate.
        _ = schedule.GetNextOccurrence("* * * * *", values.TimeZoneId, values.AvailableAt);
        if (values.ScheduleType == NotificationScheduleType.SCHEDULED)
            _ = schedule.GetNextOccurrence(values.CronString!, values.TimeZoneId, values.AvailableAt, true);
        Description = values.Description;
        IsSendNow = values.IsSendNow; ScheduleType = values.ScheduleType; CronString = values.CronString;
        Critical = values.Critical; Title = values.Title; Body = values.Body; BodyText = values.BodyText;
        AvailableAt = values.AvailableAt; ExpiredAfter = values.ExpiredAfter; MaxSendTimes = values.MaxSendTimes;
        AudienceType = values.AudienceType; InAppEnabled = values.InAppEnabled; EmailEnabled = values.EmailEnabled;
        PushEnabled = values.PushEnabled; SmsEnabled = values.SmsEnabled; Metadata = values.Metadata; TimeZoneId = values.TimeZoneId;
        _users.RemoveAll(x => !values.UserIds.Contains(x.UserId));
        _teams.RemoveAll(x => !values.TeamIds.Contains(x.TeamId));
        _users.AddRange(values.UserIds.Where(id => _users.All(x => x.UserId != id)).Select(id => new NotificationConfigUser(Id, id)));
        _teams.AddRange(values.TeamIds.Where(id => _teams.All(x => x.TeamId != id)).Select(id => new NotificationConfigTeam(Id, id)));
    }

    private static DateTimeOffset? CalculateNext(NotificationConfigValues values, INotificationSchedule schedule, DateTimeOffset now, bool first)
    {
        if (values.ExpiredAfter <= now) return null;
        if (values.IsSendNow && first) return now;
        if (values.ScheduleType == NotificationScheduleType.ONCE) return values.AvailableAt > now ? values.AvailableAt : null;
        var next = schedule.GetNextOccurrence(values.CronString!, values.TimeZoneId,
            values.AvailableAt > now ? values.AvailableAt : now, values.AvailableAt > now);
        return next >= values.ExpiredAfter ? null : next;
    }

    private void ReleaseLease() { LockedUntil = null; LeaseToken = null; }
}

public sealed class NotificationConfigUser
{
    public Guid NotificationConfigId { get; private set; }
    public Guid UserId { get; private set; }
    private NotificationConfigUser() { }
    internal NotificationConfigUser(Guid configId, Guid userId) { NotificationConfigId = configId; UserId = userId; }
}

public sealed class NotificationConfigTeam
{
    public Guid NotificationConfigId { get; private set; }
    public Guid TeamId { get; private set; }
    private NotificationConfigTeam() { }
    internal NotificationConfigTeam(Guid configId, Guid teamId) { NotificationConfigId = configId; TeamId = teamId; }
}
