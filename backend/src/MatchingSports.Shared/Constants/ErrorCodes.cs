namespace MatchingSports.Shared;

public static class ErrorCodes
{
    public const string InvalidMatchingOperation = "INVALID_MATCHING_OPERATION";
    public const string ListingNotFound = "LISTING_NOT_FOUND";
    public const string ListingRequestNotFound = "LISTING_REQUEST_NOT_FOUND";
    public const string MatchNotFound = "MATCH_NOT_FOUND";
    public const string ListingWatchNotFound = "LISTING_WATCH_NOT_FOUND";
    public const string InvalidTeamOperation = "INVALID_TEAM_OPERATION";
    public const string TeamNotFound = "TEAM_NOT_FOUND";
    public const string TeamInvitationNotFound = "TEAM_INVITATION_NOT_FOUND";
    public const string TeamJoinRequestNotFound = "TEAM_JOIN_REQUEST_NOT_FOUND";
    public const string InvalidNotificationConfig = "INVALID_NOTIFICATION_CONFIG";
    public const string NotificationConfigNotFound = "NOTIFICATION_CONFIG_NOT_FOUND";
    public const string NotificationNotFound = "NOTIFICATION_NOT_FOUND";
    public const string NotificationChannelUnavailable = "NOTIFICATION_CHANNEL_UNAVAILABLE";
    public const string InvalidSystemSettings = "INVALID_SYSTEM_SETTINGS";
    public const string SystemSettingsNotFound = "SYSTEM_SETTINGS_NOT_FOUND";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string BadRequest = "BAD_REQUEST";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string Conflict = "CONFLICT";
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    public const string InternalServerError = "INTERNAL_SERVER_ERROR";

    public const string InvalidAccessToken = "INVALID_ACCESS_TOKEN";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string InvalidRefreshToken = "INVALID_REFRESH_TOKEN";
    public const string InvalidOtp = "INVALID_OTP";
    public const string InvalidPagination = "INVALID_PAGINATION";
    public const string InvalidDateRange = "INVALID_DATE_RANGE";

    public const string GoogleAuthNotConfigured = "GOOGLE_AUTH_NOT_CONFIGURED";
    public const string GoogleAuthenticationFailed = "GOOGLE_AUTHENTICATION_FAILED";
    public const string GoogleAccountDataMissing = "GOOGLE_ACCOUNT_DATA_MISSING";

    public const string EmailAlreadyRegistered = "EMAIL_ALREADY_REGISTERED";
    public const string EmailNotVerified = "EMAIL_NOT_VERIFIED";
    public const string EmailNotConfigured = "EMAIL_NOT_CONFIGURED";
    public const string EmailSendFailed = "EMAIL_SEND_FAILED";
    public const string AccountLocked = "ACCOUNT_LOCKED";
    public const string AccountNotActive = "ACCOUNT_NOT_ACTIVE";
    public const string UserNotFound = "USER_NOT_FOUND";
    public const string UserProfileNotFound = "USER_PROFILE_NOT_FOUND";
    public const string UserSportProfileNotFound = "USER_SPORT_PROFILE_NOT_FOUND";
    public const string InvalidUserSportProfile = "INVALID_USER_SPORT_PROFILE";
    public const string InvalidRole = "INVALID_ROLE";
    public const string InvalidPermission = "INVALID_PERMISSION";
    public const string RoleNotFound = "ROLE_NOT_FOUND";
    public const string PermissionNotFound = "PERMISSION_NOT_FOUND";
    public const string InvalidSport = "INVALID_SPORT";
    public const string SportNotFound = "SPORT_NOT_FOUND";
    public const string OtpResendTooSoon = "OTP_RESEND_TOO_SOON";
    public const string OtpSendLimitReached = "OTP_SEND_LIMIT_REACHED";
    public const string IdentityValidationFailed = "IDENTITY_VALIDATION_FAILED";

    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
    public const string DuplicateResource = "DUPLICATE_RESOURCE";
    public const string ReferenceConflict = "REFERENCE_CONFLICT";
    public const string DatabaseUnavailable = "DATABASE_UNAVAILABLE";
    public const string DependencyTimeout = "DEPENDENCY_TIMEOUT";

    private const string HttpStatusPrefix = "HTTP_";

    public static string FromHttpStatus(int statusCode) =>
        $"{HttpStatusPrefix}{statusCode}";
}
