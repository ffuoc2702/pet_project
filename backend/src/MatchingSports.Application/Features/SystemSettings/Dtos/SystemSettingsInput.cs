using MatchingSports.Domain.ValueObjects;

namespace MatchingSports.Application.Features.SystemSettings;

public sealed record SystemSettingsInput(
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
    int NotificationRetentionDays)
{
    internal SystemSettingsValues ToDomainValues() => new(OutboxMaxRetryCount, MaxCaptainTeamsPerUser,
        MaxMemberTeamsPerUser, MaxDeputiesPerTeam, MaxMembersPerTeam, TeamInvitationLifetimeHours,
        MaxPendingTeamRequests, OutboxRetryInitialDelaySeconds, OutboxRetryMaxDelaySeconds,
        ProcessedOutboxRetentionDays, NotificationRetentionDays);
}
