using System.Security.Claims;
using MatchingSports.Api.Contracts;
using MatchingSports.Application.Common.Exceptions;
using MatchingSports.Application.Features.Identity.ExternalLogin;
using MatchingSports.Application.Features.Identity.GetCurrentUser;
using MatchingSports.Application.Features.Identity.Login;
using MatchingSports.Application.Features.Identity.Logout;
using MatchingSports.Application.Features.Identity.Refresh;
using MatchingSports.Application.Features.Identity.Register;
using MatchingSports.Application.Features.Identity.RequestPasswordReset;
using MatchingSports.Application.Features.Identity.ResendVerification;
using MatchingSports.Application.Features.Identity.ResetPassword;
using MatchingSports.Application.Features.Identity.VerifyEmail;
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
                "GOOGLE_AUTH_NOT_CONFIGURED",
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
                "GOOGLE_AUTHENTICATION_FAILED",
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
                    "GOOGLE_ACCOUNT_DATA_MISSING",
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

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<BaseResponse<CurrentUserResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<CurrentUserResponse>>> Me(
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            throw new UnauthorizedException("INVALID_ACCESS_TOKEN", "Access token không hợp lệ.");

        var result = await mediator.Send(new GetCurrentUserQuery(userId), cancellationToken);
        return OkResponse(CurrentUserResponse.From(result));
    }

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? GetUserAgent()
    {
        var value = Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
