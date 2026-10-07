namespace MatchingSports.Application.Features.SystemSettings;

public sealed record SystemSettingsDto(
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
    Guid ConcurrencyToken,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
