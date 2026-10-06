using System.ComponentModel.DataAnnotations;
using MatchingSports.Application.Abstractions;

namespace MatchingSports.Api.Contracts;

public sealed class RegisterRequest
{
    [Required(ErrorMessage = "Trường {0} là bắt buộc."), EmailAddress(ErrorMessage = "Trường {0} phải là địa chỉ email hợp lệ."), MaxLength(256, ErrorMessage = "Trường {0} không được vượt quá {1} ký tự hoặc phần tử.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Trường {0} là bắt buộc."), MinLength(10, ErrorMessage = "Trường {0} phải có ít nhất {1} ký tự hoặc phần tử."), MaxLength(128, ErrorMessage = "Trường {0} không được vượt quá {1} ký tự hoặc phần tử.")]
    public string Password { get; init; } = string.Empty;

    [Required(ErrorMessage = "Trường {0} là bắt buộc."), MinLength(2, ErrorMessage = "Trường {0} phải có ít nhất {1} ký tự hoặc phần tử."), MaxLength(100, ErrorMessage = "Trường {0} không được vượt quá {1} ký tự hoặc phần tử.")]
    public string DisplayName { get; init; } = string.Empty;
}

public sealed class EmailOtpRequest
{
    [Required(ErrorMessage = "Trường {0} là bắt buộc."), EmailAddress(ErrorMessage = "Trường {0} phải là địa chỉ email hợp lệ."), MaxLength(256, ErrorMessage = "Trường {0} không được vượt quá {1} ký tự hoặc phần tử.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Trường {0} là bắt buộc."), RegularExpression("^[0-9]{6}$", ErrorMessage = "Trường {0} không đúng định dạng yêu cầu.")]
    public string Otp { get; init; } = string.Empty;
}

public sealed class EmailRequest
{
    [Required(ErrorMessage = "Trường {0} là bắt buộc."), EmailAddress(ErrorMessage = "Trường {0} phải là địa chỉ email hợp lệ."), MaxLength(256, ErrorMessage = "Trường {0} không được vượt quá {1} ký tự hoặc phần tử.")]
    public string Email { get; init; } = string.Empty;
}

public sealed class LoginRequest
{
    [Required(ErrorMessage = "Trường {0} là bắt buộc."), EmailAddress(ErrorMessage = "Trường {0} phải là địa chỉ email hợp lệ."), MaxLength(256, ErrorMessage = "Trường {0} không được vượt quá {1} ký tự hoặc phần tử.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Trường {0} là bắt buộc."), MaxLength(128, ErrorMessage = "Trường {0} không được vượt quá {1} ký tự hoặc phần tử.")]
    public string Password { get; init; } = string.Empty;
}

public sealed class RefreshTokenRequest
{
    [Required(ErrorMessage = "Trường {0} là bắt buộc."), MaxLength(512, ErrorMessage = "Trường {0} không được vượt quá {1} ký tự hoặc phần tử.")]
    public string RefreshToken { get; init; } = string.Empty;
}

public sealed class ResetPasswordRequest
{
    [Required(ErrorMessage = "Trường {0} là bắt buộc."), EmailAddress(ErrorMessage = "Trường {0} phải là địa chỉ email hợp lệ."), MaxLength(256, ErrorMessage = "Trường {0} không được vượt quá {1} ký tự hoặc phần tử.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Trường {0} là bắt buộc."), RegularExpression("^[0-9]{6}$", ErrorMessage = "Trường {0} không đúng định dạng yêu cầu.")]
    public string Otp { get; init; } = string.Empty;

    [Required(ErrorMessage = "Trường {0} là bắt buộc."), MinLength(10, ErrorMessage = "Trường {0} phải có ít nhất {1} ký tự hoặc phần tử."), MaxLength(128, ErrorMessage = "Trường {0} không được vượt quá {1} ký tự hoặc phần tử.")]
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
    public IReadOnlyCollection<MatchingSports.Application.Features.UserSportProfile.UserSportProfileDto> SportProfiles { get; init; } = [];

    public static CurrentUserResponse From(CurrentUserResult result) => new(
        result.Id,
        result.Email,
        result.DisplayName,
        result.EmailVerified,
        result.Roles,
        result.Permissions)
    { SportProfiles = result.SportProfiles };
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
