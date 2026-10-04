using System.ComponentModel.DataAnnotations;
using MatchingSports.Application.Abstractions;

namespace MatchingSports.Api.Contracts;

public sealed class RegisterRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(10), MaxLength(128)]
    public string Password { get; init; } = string.Empty;

    [Required, MinLength(2), MaxLength(100)]
    public string DisplayName { get; init; } = string.Empty;
}

public sealed class EmailOtpRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, RegularExpression("^[0-9]{6}$")]
    public string Otp { get; init; } = string.Empty;
}

public sealed class EmailRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = string.Empty;
}

public sealed class LoginRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, MaxLength(128)]
    public string Password { get; init; } = string.Empty;
}

public sealed class RefreshTokenRequest
{
    [Required, MaxLength(512)]
    public string RefreshToken { get; init; } = string.Empty;
}

public sealed class ResetPasswordRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, RegularExpression("^[0-9]{6}$")]
    public string Otp { get; init; } = string.Empty;

    [Required, MinLength(10), MaxLength(128)]
    public string NewPassword { get; init; } = string.Empty;
}

public sealed record RegistrationResponse(Guid UserId, string Email, DateTimeOffset OtpExpiresAt)
{
    public static RegistrationResponse From(RegistrationResult result) =>
        new(result.UserId, result.Email, result.OtpExpiresAt);
}

public sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    bool EmailVerified,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions)
{
    public static CurrentUserResponse From(CurrentUserResult result) => new(
        result.Id,
        result.Email,
        result.DisplayName,
        result.EmailVerified,
        result.Roles,
        result.Permissions);
}

public sealed record AuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt,
    CurrentUserResponse User)
{
    public static AuthTokenResponse From(AuthTokenResult result) => new(
        result.AccessToken,
        result.RefreshToken,
        result.TokenType,
        result.AccessTokenExpiresAt,
        result.RefreshTokenExpiresAt,
        CurrentUserResponse.From(result.User));
}
