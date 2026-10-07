namespace MatchingSports.Domain.Entities;

public sealed class OutboxMessage : AuditableEntity
{
    public Guid Id { get; set; }
    public string AggregateType { get; set; } = null!;
    public Guid AggregateId { get; set; }
    public string EventType { get; set; } = null!;
    public string PayloadJson { get; set; } = null!;
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset AvailableAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public DateTimeOffset PurgeAfter { get; private set; }
    public DateTimeOffset? FailedAt { get; private set; }

    public void InitializeRetention(SystemSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (CreatedAt == default)
            throw new InvalidOperationException("Phải xác định thời điểm tạo message trước khi tính thời điểm dọn dữ liệu.");
        PurgeAfter = settings.GetProcessedOutboxPurgeAfter(CreatedAt);
    }

    public bool CanProcess(DateTimeOffset now) =>
        ProcessedAt is null && FailedAt is null && AvailableAt <= now && (LockedUntil is null || LockedUntil <= now);

    public void RecordDeliveryFailure(string error, SystemSettings settings, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        if (ProcessedAt is not null || FailedAt is not null)
            throw new InvalidOperationException("Message đã kết thúc xử lý.");
        if (RetryCount < 0)
            throw new InvalidOperationException("Số lần thử lại không được âm.");

        var nextAvailableAt = RetryCount < settings.OutboxMaxRetryCount
            ? now.Add(settings.GetOutboxRetryDelay(RetryCount + 1)) : (DateTimeOffset?)null;
        LastError = error;
        LockedUntil = null;
        if (nextAvailableAt is null)
            FailedAt = now;
        else
        {
            RetryCount++;
            AvailableAt = nextAvailableAt.Value;
        }
    }

    public void MarkProcessed(DateTimeOffset now)
    {
        if (ProcessedAt is not null || FailedAt is not null)
            throw new InvalidOperationException("Message đã kết thúc xử lý.");
        ProcessedAt = now;
        LockedUntil = null;
        LastError = null;
    }
}
