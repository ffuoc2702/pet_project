// Enum names are persisted values and HTTP contracts shared with web/mobile.
#pragma warning disable CA1707
namespace MatchingSports.Domain.Enums;

public enum NotificationScheduleType { ONCE, SCHEDULED }
public enum NotificationCritical { INFO, WARN, CRIT }
public enum NotificationAudienceType { SELECTED_USERS, SELECTED_TEAMS, ALL_USERS }
public enum NotificationConfigStatus { DRAFT, ACTIVE, CANCEL }
public enum NotificationSchedulingStatus { PENDING, SCHEDULING, COMPLETED, ABORTED }
public enum NotificationDeliveryStatus { DISABLED, PENDING, SENT, FAILED, SKIPPED }
public enum NotificationChannel { EMAIL, PUSH, SMS }
