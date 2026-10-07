using MatchingSports.Domain.ValueObjects;

namespace MatchingSports.Domain.Entities;

public sealed class SystemSettings : IHasConcurrencyToken
{
    public static Guid SingletonId { get; } = Guid.Parse("00000000-0000-0000-0002-000000000001");

    private SystemSettings() { }

    public Guid Id { get; private set; }
    public int OutboxMaxRetryCount { get; private set; }
    public int MaxCaptainTeamsPerUser { get; private set; }
    public int MaxMemberTeamsPerUser { get; private set; }
    public int MaxDeputiesPerTeam { get; private set; }
    public int MaxMembersPerTeam { get; private set; }
    public int TeamInvitationLifetimeHours { get; private set; }
    public int MaxPendingTeamRequests { get; private set; }
    public int OutboxRetryInitialDelaySeconds { get; private set; }
    public int OutboxRetryMaxDelaySeconds { get; private set; }
    public int ProcessedOutboxRetentionDays { get; private set; }
    public int NotificationRetentionDays { get; private set; }
    public Guid ConcurrencyToken { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static SystemSettings CreateDefaults()
    {
        var settings = new SystemSettings { Id = SingletonId };
        settings.UpdateValues(SystemSettingsValues.Defaults);
        return settings;
    }

    public void UpdateValues(SystemSettingsValues values)
    {
        ArgumentNullException.ThrowIfNull(values);
        values.Validate();

        OutboxMaxRetryCount = values.OutboxMaxRetryCount;
        MaxCaptainTeamsPerUser = values.MaxCaptainTeamsPerUser;
        MaxMemberTeamsPerUser = values.MaxMemberTeamsPerUser;
        MaxDeputiesPerTeam = values.MaxDeputiesPerTeam;
        MaxMembersPerTeam = values.MaxMembersPerTeam;
        TeamInvitationLifetimeHours = values.TeamInvitationLifetimeHours;
        MaxPendingTeamRequests = values.MaxPendingTeamRequests;
        OutboxRetryInitialDelaySeconds = values.OutboxRetryInitialDelaySeconds;
        OutboxRetryMaxDelaySeconds = values.OutboxRetryMaxDelaySeconds;
        ProcessedOutboxRetentionDays = values.ProcessedOutboxRetentionDays;
        NotificationRetentionDays = values.NotificationRetentionDays;
        // Even an unchanged submission must participate in optimistic concurrency and auditing.
        ConcurrencyToken = Guid.NewGuid();
    }

    public SystemSettingsValues GetValues() => new(OutboxMaxRetryCount, MaxCaptainTeamsPerUser,
        MaxMemberTeamsPerUser, MaxDeputiesPerTeam, MaxMembersPerTeam, TeamInvitationLifetimeHours,
        MaxPendingTeamRequests, OutboxRetryInitialDelaySeconds, OutboxRetryMaxDelaySeconds,
        ProcessedOutboxRetentionDays, NotificationRetentionDays);

    public TimeSpan GetOutboxRetryDelay(int retryNumber)
    {
        if (retryNumber < 1 || retryNumber > OutboxMaxRetryCount)
            throw new ArgumentOutOfRangeException(nameof(retryNumber), "Số lần thử lại vượt quá giới hạn cấu hình.");

        // Clamp before doubling to avoid overflow, including for large retry numbers.
        var seconds = OutboxRetryInitialDelaySeconds;
        for (var index = 1; index < retryNumber && seconds < OutboxRetryMaxDelaySeconds; index++)
            seconds = seconds > OutboxRetryMaxDelaySeconds / 2 ? OutboxRetryMaxDelaySeconds : seconds * 2;
        return TimeSpan.FromSeconds(seconds);
    }

    public DateTimeOffset GetTeamInvitationExpiresAt(DateTimeOffset createdAt) =>
        createdAt.AddHours(TeamInvitationLifetimeHours);

    public DateTimeOffset GetProcessedOutboxPurgeAfter(DateTimeOffset createdAt) =>
        createdAt.AddDays(ProcessedOutboxRetentionDays);

    public DateTimeOffset GetNotificationPurgeAfter(DateTimeOffset createdAt) =>
        createdAt.AddDays(NotificationRetentionDays);

    public void EnsureCanCaptainAnotherTeam(int activeCaptainTeamCount) =>
        RequireAvailableCapacity(activeCaptainTeamCount, MaxCaptainTeamsPerUser, "Đã đạt giới hạn đội được làm đội trưởng.");

    public void EnsureCanJoinAnotherTeam(int activeMemberTeamCount, int activeTeamMemberCount)
    {
        RequireAvailableCapacity(activeMemberTeamCount, MaxMemberTeamsPerUser, "Đã đạt giới hạn đội được tham gia.");
        RequireAvailableCapacity(activeTeamMemberCount, MaxMembersPerTeam, "Đội đã đạt giới hạn thành viên.");
    }

    public void EnsureCanDelegateAnotherDeputy(int activeDeputyCount) =>
        RequireAvailableCapacity(activeDeputyCount, MaxDeputiesPerTeam, "Đội đã đạt giới hạn đội phó.");

    public void EnsureCanCreateTeamRequest(int pendingInvitationAndRequestCount) =>
        RequireAvailableCapacity(pendingInvitationAndRequestCount, MaxPendingTeamRequests, "Đội đã đạt giới hạn lời mời/yêu cầu đang chờ.");

    private static void RequireAvailableCapacity(int count, int limit, string message)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Số lượng hiện tại không được âm.");
        if (count >= limit)
            throw new ArgumentException(message, nameof(count));
    }
}
