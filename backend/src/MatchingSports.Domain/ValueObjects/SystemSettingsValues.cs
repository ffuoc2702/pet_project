namespace MatchingSports.Domain.ValueObjects;

public sealed record SystemSettingsValues(
    int OutboxMaxRetryCount,
    int MaxCaptainTeamsPerUser,
    int MaxMemberTeamsPerUser,
    int MaxDeputiesPerTeam,
    int MaxMembersPerTeam,
    int TeamInvitationLifetimeHours,
    int MaxPendingTeamRequests,
    int OutboxRetryInitialDelaySeconds,
    int OutboxRetryMaxDelaySeconds,
    int ProcessedOutboxRetentionDays,
    int NotificationRetentionDays,
    int MaxActiveListingWatchesPerUser = 20,
    int MaxListingSearchRadiusMeters = 50000,
    int MaxListingWatchWindowDays = 30,
    int MatchReminderLeadMinutes = 60)
{
    public static SystemSettingsValues Defaults { get; } = new(5, 3, 10, 2, 50, 72, 100, 5, 3600, 30, 90);

    public void Validate()
    {
        RequireRange(OutboxMaxRetryCount, 0, 100, nameof(OutboxMaxRetryCount));
        RequireRange(MaxCaptainTeamsPerUser, 0, 10000, nameof(MaxCaptainTeamsPerUser));
        RequireRange(MaxMemberTeamsPerUser, 0, 10000, nameof(MaxMemberTeamsPerUser));
        RequireRange(MaxMembersPerTeam, 1, 10000, nameof(MaxMembersPerTeam));
        RequireRange(MaxDeputiesPerTeam, 0, MaxMembersPerTeam - 1, nameof(MaxDeputiesPerTeam));
        RequireRange(TeamInvitationLifetimeHours, 1, 8760, nameof(TeamInvitationLifetimeHours));
        RequireRange(MaxPendingTeamRequests, 0, 10000, nameof(MaxPendingTeamRequests));
        RequireRange(OutboxRetryInitialDelaySeconds, 1, 604800, nameof(OutboxRetryInitialDelaySeconds));
        RequireRange(OutboxRetryMaxDelaySeconds, OutboxRetryInitialDelaySeconds, 604800, nameof(OutboxRetryMaxDelaySeconds));
        RequireRange(ProcessedOutboxRetentionDays, 1, 36500, nameof(ProcessedOutboxRetentionDays));
        RequireRange(NotificationRetentionDays, 1, 36500, nameof(NotificationRetentionDays));
        RequireRange(MaxActiveListingWatchesPerUser, 0, 1000, nameof(MaxActiveListingWatchesPerUser));
        RequireRange(MaxListingSearchRadiusMeters, 1, 200000, nameof(MaxListingSearchRadiusMeters));
        RequireRange(MaxListingWatchWindowDays, 1, 365, nameof(MaxListingWatchWindowDays));
        RequireRange(MatchReminderLeadMinutes, 1, 10080, nameof(MatchReminderLeadMinutes));
    }

    private static void RequireRange(int value, int minimum, int maximum, string name)
    {
        if (value < minimum || value > maximum)
            throw new ArgumentOutOfRangeException(name, $"Giá trị phải từ {minimum} đến {maximum}.");
    }
}
