using MatchingSports.Application.Common;

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
    int MaxActiveListingWatchesPerUser = 20,
    int MaxListingSearchRadiusMeters = 50000,
    int MaxListingWatchWindowDays = 30,
    int MatchReminderLeadMinutes = 60) : ConcurrentBaseDto;
