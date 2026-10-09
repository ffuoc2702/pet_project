using System.ComponentModel.DataAnnotations;

namespace MatchingSports.Api.Contracts;

public sealed class UpdateSystemSettingsRequest
{
    [Required(ErrorMessage = "Trường {0} là bắt buộc."), Range(0, 100, ErrorMessage = "Trường {0} phải từ {1} đến {2}.")]
    public int? OutboxMaxRetryCount { get; init; }
    [Required(ErrorMessage = "Trường {0} là bắt buộc."), Range(0, 10000, ErrorMessage = "Trường {0} phải từ {1} đến {2}.")]
    public int? MaxCaptainTeamsPerUser { get; init; }
    [Required(ErrorMessage = "Trường {0} là bắt buộc."), Range(0, 10000, ErrorMessage = "Trường {0} phải từ {1} đến {2}.")]
    public int? MaxMemberTeamsPerUser { get; init; }
    [Required(ErrorMessage = "Trường {0} là bắt buộc."), Range(0, 9999, ErrorMessage = "Trường {0} phải từ {1} đến {2}.")]
    public int? MaxDeputiesPerTeam { get; init; }
    [Required(ErrorMessage = "Trường {0} là bắt buộc."), Range(1, 10000, ErrorMessage = "Trường {0} phải từ {1} đến {2}.")]
    public int? MaxMembersPerTeam { get; init; }
    [Required(ErrorMessage = "Trường {0} là bắt buộc."), Range(1, 8760, ErrorMessage = "Trường {0} phải từ {1} đến {2}.")]
    public int? TeamInvitationLifetimeHours { get; init; }
    [Required(ErrorMessage = "Trường {0} là bắt buộc."), Range(0, 10000, ErrorMessage = "Trường {0} phải từ {1} đến {2}.")]
    public int? MaxPendingTeamRequests { get; init; }
    [Required(ErrorMessage = "Trường {0} là bắt buộc."), Range(1, 604800, ErrorMessage = "Trường {0} phải từ {1} đến {2}.")]
    public int? OutboxRetryInitialDelaySeconds { get; init; }
    [Required(ErrorMessage = "Trường {0} là bắt buộc."), Range(1, 604800, ErrorMessage = "Trường {0} phải từ {1} đến {2}.")]
    public int? OutboxRetryMaxDelaySeconds { get; init; }
    [Required(ErrorMessage = "Trường {0} là bắt buộc."), Range(1, 36500, ErrorMessage = "Trường {0} phải từ {1} đến {2}.")]
    public int? ProcessedOutboxRetentionDays { get; init; }
    [Required(ErrorMessage = "Trường {0} là bắt buộc."), Range(1, 36500, ErrorMessage = "Trường {0} phải từ {1} đến {2}.")]
    public int? NotificationRetentionDays { get; init; }
    [Required(ErrorMessage = "Trường {0} là bắt buộc.")]
    public Guid? ConcurrencyToken { get; init; }
    [Required(ErrorMessage = "Trường {0} là bắt buộc."), Range(0, 1000, ErrorMessage = "Trường {0} phải từ {1} đến {2}.")]
    public int? MaxActiveListingWatchesPerUser { get; init; }
    [Required(ErrorMessage = "Trường {0} là bắt buộc."), Range(1, 200000, ErrorMessage = "Trường {0} phải từ {1} đến {2}.")]
    public int? MaxListingSearchRadiusMeters { get; init; }
    [Required(ErrorMessage = "Trường {0} là bắt buộc."), Range(1, 365, ErrorMessage = "Trường {0} phải từ {1} đến {2}.")]
    public int? MaxListingWatchWindowDays { get; init; }
    [Required(ErrorMessage = "Trường {0} là bắt buộc."), Range(1, 10080, ErrorMessage = "Trường {0} phải từ {1} đến {2}.")]
    public int? MatchReminderLeadMinutes { get; init; }
}
