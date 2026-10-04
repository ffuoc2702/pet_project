namespace MatchingSports.Application.Abstractions;

public interface IIdentityService
{
    Task<RegistrationResult> RegisterAsync(
        string email,
        string password,
        string displayName,
        string? ipAddress,
        CancellationToken cancellationToken);

    Task VerifyEmailAsync(
        string email,
        string otp,
        CancellationToken cancellationToken);

    Task ResendVerificationOtpAsync(
        string email,
        string? ipAddress,
        CancellationToken cancellationToken);

    Task<AuthTokenResult> LoginAsync(
        string email,
        string password,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task<AuthTokenResult> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);

    Task RequestPasswordResetAsync(
        string email,
        string? ipAddress,
        CancellationToken cancellationToken);

    Task ResetPasswordAsync(
        string email,
        string otp,
        string newPassword,
        CancellationToken cancellationToken);

    Task<AuthTokenResult> ExternalLoginAsync(
        string provider,
        string providerKey,
        string email,
        string displayName,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task<CurrentUserResult> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed record RegistrationResult(Guid UserId, string Email, DateTimeOffset OtpExpiresAt);

public sealed record AuthTokenResult(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt,
    CurrentUserResult User);

public sealed record CurrentUserResult(
    Guid Id,
    string Email,
    string DisplayName,
    bool EmailVerified,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
