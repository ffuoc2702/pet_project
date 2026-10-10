using System.Security.Claims;
using MatchingSports.Api.Contracts;
using MatchingSports.Api.Identity;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Identity;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MatchingSports.Api.Controllers;

[Route("api/v1/auth/web")]
[EnableRateLimiting("AuthStrict")]
public sealed class WebAuthController(IMediator mediator, IAntiforgery antiforgery,
    IAuthenticationSchemeProvider authenticationSchemes, IWebHostEnvironment environment)
    : ApiControllerBase
{
    private string RefreshCookieName => environment.IsDevelopment() ? "matchingsports.refresh" : "__Host-matchingsports.refresh";

    [HttpGet("csrf")]
    public ActionResult<BaseResponse<WebCsrfResponse>> Csrf()
    {
        return OkResponse(new WebCsrfResponse(antiforgery.GetAndStoreTokens(HttpContext).RequestToken!));
    }

    [HttpPost("login"), WebCsrf]
    [ProducesResponseType<BaseResponse<WebAuthTokenResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<WebAuthTokenResponse>>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new LoginCommand(request.Email, request.Password,
            GetIpAddress(), GetUserAgent()), cancellationToken);
        return CompleteLogin(result);
    }

    [HttpPost("refresh"), WebCsrf]
    [ProducesResponseType<BaseResponse<WebAuthTokenResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<WebAuthTokenResponse>>> Refresh(CancellationToken cancellationToken)
    {
        var token = Request.Cookies[RefreshCookieName];
        if (string.IsNullOrWhiteSpace(token))
            throw new UnauthorizedException(ErrorCodes.InvalidRefreshToken, "Phiên đăng nhập đã hết hạn.");
        try
        {
            var result = await mediator.Send(new RefreshTokenCommand(token,
                GetIpAddress(), GetUserAgent()), cancellationToken);
            return CompleteLogin(result);
        }
        catch (UnauthorizedException)
        {
            Response.Cookies.Delete(RefreshCookieName, CookieOptions());
            throw;
        }
    }

    [HttpPost("logout"), WebCsrf]
    public async Task<ActionResult<BaseResponse<object>>> Logout(CancellationToken cancellationToken)
    {
        if (Request.Cookies[RefreshCookieName] is { Length: > 0 } token)
            await mediator.Send(new LogoutCommand(token), cancellationToken);
        Response.Cookies.Delete(RefreshCookieName, CookieOptions());
        Response.Headers.CacheControl = "no-store";
        return OkResponse<object>(new { });
    }

    [HttpGet("google")]
    public async Task<ChallengeResult> GoogleLogin()
    {
        if (await authenticationSchemes.GetSchemeAsync("Google") is null)
            throw new ServiceUnavailableException(ErrorCodes.GoogleAuthNotConfigured, "Đăng nhập Google chưa được cấu hình.");
        return Challenge(new AuthenticationProperties
        {
            RedirectUri = Url.ActionLink(nameof(GoogleCallback))
                ?? throw new InvalidOperationException("Không thể tạo URL callback.")
        }, ["Google"]);
    }

    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback(CancellationToken cancellationToken)
    {
        var authentication = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
        if (!authentication.Succeeded || authentication.Principal is null)
            throw new UnauthorizedException(ErrorCodes.GoogleAuthenticationFailed, "Không thể xác thực tài khoản Google.");
        try
        {
            var principal = authentication.Principal;
            var providerKey = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(providerKey) || string.IsNullOrWhiteSpace(email))
                throw new UnauthorizedException(ErrorCodes.GoogleAccountDataMissing, "Google không cung cấp đủ thông tin.");
            var result = await mediator.Send(new ExternalLoginCommand("Google", providerKey, email,
                principal.FindFirstValue(ClaimTypes.Name) ?? email, GetIpAddress(), GetUserAgent()), cancellationToken);
            SetRefreshCookie(result);
            // A fixed local redirect prevents open redirects and keeps tokens out of browser history.
            return LocalRedirect("/auth/callback");
        }
        finally { await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme); }
    }

    private ActionResult<BaseResponse<WebAuthTokenResponse>> CompleteLogin(AuthTokenResult result)
    {
        SetRefreshCookie(result);
        return OkResponse(new WebAuthTokenResponse(result.AccessToken, result.TokenType,
            result.AccessTokenExpiresAt, CurrentUserResponse.From(result.User)));
    }

    private void SetRefreshCookie(AuthTokenResult result)
    {
        var options = CookieOptions();
        options.Expires = result.RefreshTokenExpiresAt;
        Response.Cookies.Append(RefreshCookieName, result.RefreshToken, options);
        Response.Headers.CacheControl = "no-store";
    }

    private CookieOptions CookieOptions() => new()
    {
        HttpOnly = true,
        Secure = !environment.IsDevelopment() || Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        IsEssential = true
    };

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
    private string? GetUserAgent() => Request.Headers.UserAgent.ToString() is { Length: > 0 } value ? value : null;
}
