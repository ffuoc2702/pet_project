using System.Security.Claims;
using MatchingSports.Api.Contracts;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Identity;
using MatchingSports.Application.Features.UserProfile;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MatchingSports.Api.Controllers;

[Route("api/v1/auth")]
[EnableRateLimiting("AuthStrict")]
public sealed class AuthController(
    IMediator mediator,
    IAuthenticationSchemeProvider authenticationSchemeProvider) : ApiControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<BaseResponse<RegistrationResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<RegistrationResponse>>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RegisterCommand(
            request.Email,
            request.Password,
            request.DisplayName,
            GetIpAddress()), cancellationToken);

        return OkResponse(
            RegistrationResponse.From(result),
            "Đăng ký thành công. Mã OTP đã được gửi tới email của bạn.");
    }

    [HttpPost("verify-email")]
    public async Task<ActionResult<BaseResponse<object>>> VerifyEmail(
        EmailOtpRequest request,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new VerifyEmailCommand(request.Email, request.Otp), cancellationToken);
        return OkResponse<object>(new { }, "Xác thực email thành công.");
    }

    [HttpPost("resend-verification")]
    public async Task<ActionResult<BaseResponse<object>>> ResendVerification(
        EmailRequest request,
        CancellationToken cancellationToken)
    {
        await mediator.Send(
            new ResendVerificationCommand(request.Email, GetIpAddress()),
            cancellationToken);
        return OkResponse<object>(new { }, "Nếu email hợp lệ, mã OTP mới đã được gửi.");
    }

    [HttpPost("login")]
    [ProducesResponseType<BaseResponse<AuthTokenResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<AuthTokenResponse>>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new LoginCommand(
            request.Email,
            request.Password,
            GetIpAddress(),
            GetUserAgent()), cancellationToken);
        return OkResponse(AuthTokenResponse.From(result));
    }

    [HttpPost("refresh")]
    [ProducesResponseType<BaseResponse<AuthTokenResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<AuthTokenResponse>>> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RefreshTokenCommand(
            request.RefreshToken,
            GetIpAddress(),
            GetUserAgent()), cancellationToken);
        return OkResponse(AuthTokenResponse.From(result));
    }

    [HttpPost("logout")]
    public async Task<ActionResult<BaseResponse<object>>> Logout(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new LogoutCommand(request.RefreshToken), cancellationToken);
        return OkResponse<object>(new { }, "Đăng xuất thành công.");
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<BaseResponse<object>>> ForgotPassword(
        EmailRequest request,
        CancellationToken cancellationToken)
    {
        await mediator.Send(
            new RequestPasswordResetCommand(request.Email, GetIpAddress()),
            cancellationToken);
        return OkResponse<object>(new { }, "Nếu email hợp lệ, mã OTP đặt lại mật khẩu đã được gửi.");
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<BaseResponse<object>>> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await mediator.Send(
            new ResetPasswordCommand(request.Email, request.Otp, request.NewPassword),
            cancellationToken);
        return OkResponse<object>(new { }, "Đặt lại mật khẩu thành công.");
    }

    [HttpGet("google")]
    public async Task<ChallengeResult> GoogleLogin()
    {
        if (await authenticationSchemeProvider.GetSchemeAsync("Google") is null)
        {
            throw new ServiceUnavailableException(
                ErrorCodes.GoogleAuthNotConfigured,
                "Đăng nhập Google chưa được cấu hình.");
        }

        var callbackUrl = Url.ActionLink(nameof(GoogleCallback), values: null)
            ?? throw new InvalidOperationException("Cannot create Google callback URL.");
        return Challenge(new AuthenticationProperties { RedirectUri = callbackUrl }, ["Google"]);
    }

    [HttpGet("google/callback")]
    [ProducesResponseType<BaseResponse<AuthTokenResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<AuthTokenResponse>>> GoogleCallback(
        CancellationToken cancellationToken)
    {
        var authentication = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
        if (!authentication.Succeeded || authentication.Principal is null)
        {
            throw new UnauthorizedException(
                ErrorCodes.GoogleAuthenticationFailed,
                "Không thể xác thực tài khoản Google.");
        }

        try
        {
            var principal = authentication.Principal;
            var providerKey = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(providerKey) || string.IsNullOrWhiteSpace(email))
            {
                throw new UnauthorizedException(
                    ErrorCodes.GoogleAccountDataMissing,
                    "Google không cung cấp đủ thông tin tài khoản cần thiết.");
            }

            var result = await mediator.Send(new ExternalLoginCommand(
                "Google",
                providerKey,
                email,
                principal.FindFirstValue(ClaimTypes.Name) ?? email,
                GetIpAddress(),
                GetUserAgent()), cancellationToken);
            return OkResponse(AuthTokenResponse.From(result));
        }
        finally
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }
    }

    [Authorize(Policy = PermissionCodes.ReadOwnProfile)]
    [HttpGet("me")]
    [ProducesResponseType<BaseResponse<CurrentUserResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<CurrentUserResponse>>> Me(
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCurrentUserQuery(), cancellationToken);
        return OkResponse(CurrentUserResponse.From(result));
    }

    [Authorize(Policy = PermissionCodes.UpdateOwnProfile)]
    [HttpPut("me")]
    [ProducesResponseType<BaseResponse<UserProfileDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseResponse<UserProfileDto>>> UpdateUserProfile(
        [FromBody] UpdateUserProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new UpdateUserProfileCommand(
            request.DisplayName,
            request.AvatarUrl,
            request.DefaultArea,
            request.ContactNote), cancellationToken);

        return OkResponse(
            result,
            "Cập nhật hồ sơ thành công.");
    }

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? GetUserAgent()
    {
        var value = Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
