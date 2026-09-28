namespace MatchingSports.Domain.Enums;

public enum UserStatus { ACTIVE, SUSPENDED, DELETED }
public enum SportLevel { BEGINNER, INTERMEDIATE, ADVANCED }
public enum OtpPurpose { LOGIN, PHONE_CHANGE }
public enum TeamStatus { ACTIVE, INACTIVE, ARCHIVED }
public enum TeamRole { CAPTAIN, MEMBER }
public enum MemberStatus { ACTIVE, LEFT, REMOVED }
public enum ListingMode { FIND_OPPONENT, FIND_PLAYERS }
public enum ListingStatus { DRAFT, OPEN, MATCHED, CONFIRMED, COMPLETED, CANCELLED, EXPIRED }
public enum RequestType { PLAYER, OPPONENT }
public enum RequestStatus { PENDING, ACCEPTED, REJECTED, WITHDRAWN, EXPIRED }
public enum MatchStatus { PENDING, CONFIRMED, COMPLETED, CANCELLED }
public enum ParticipantRole { HOST, OPPONENT, PLAYER }
public enum ParticipantStatus { CONFIRMED, CANCELLED, NO_SHOW, COMPLETED }
public enum ReportTargetType { USER, LISTING, MATCH }
public enum ReportCategory { NO_SHOW, HARASSMENT, SPAM, FRAUD, INACCURATE_INFO, OTHER }
public enum ReportStatus { OPEN, IN_REVIEW, RESOLVED, DISMISSED }
public enum NotificationType
{
    REQUEST_CREATED,
    REQUEST_ACCEPTED,
    REQUEST_REJECTED,
    LISTING_UPDATED,
    MATCH_CANCELLED,
    MATCH_REMINDER,
    REPORT_RESOLVED
}
public enum AdminActionType
{
    WARN_USER,
    SUSPEND_USER,
    REACTIVATE_USER,
    HIDE_LISTING,
    CANCEL_MATCH,
    RESOLVE_REPORT
}
