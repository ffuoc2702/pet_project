using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MatchingSports.Api.Controllers;
using MatchingSports.Api.Identity;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Features.Identity;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MatchingSports.Tests;

public sealed class WebAuthHttpTests
{
    [Fact]
    public async Task WebAuthRequiresCsrfKeepsRefreshTokenOutOfJsonAndRotatesCookie()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Logging.ClearProviders();
        var identity = new FakeIdentity();
        builder.Services.AddScoped<IMediator>(provider => new Mediator(provider));
        builder.Services.AddScoped<IRequestHandler<LoginCommand, AuthTokenResult>, LoginCommandHandler>();
        builder.Services.AddScoped<IRequestHandler<RefreshTokenCommand, AuthTokenResult>, RefreshTokenCommandHandler>();
        builder.Services.AddScoped<IRequestHandler<LogoutCommand>, LogoutCommandHandler>();
        builder.Services.AddSingleton<IIdentityService>(identity);
        builder.Services.AddAuthentication();
        builder.Services.AddWebAuthentication(builder.Environment);
        builder.Services.AddControllers().AddApplicationPart(typeof(WebAuthController).Assembly);
        await using var app = builder.Build();
        app.Urls.Add("http://127.0.0.1:0");
        app.MapControllers();
        await app.StartAsync();
        try
        {
            using var handler = new HttpClientHandler { CookieContainer = new CookieContainer() };
            using var client = new HttpClient(handler) { BaseAddress = new Uri(app.Urls.Single()) };
            using var denied = await client.PostAsJsonAsync("/api/v1/auth/web/login", new { email = "user@example.test", password = "Password123!" });
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            Assert.Equal(0, identity.Logins);
            var deniedJson = await denied.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("INVALID_CSRF_TOKEN", deniedJson.GetProperty("code").GetString());
            using var csrfResponse = await client.GetAsync("/api/v1/auth/web/csrf");
            csrfResponse.EnsureSuccessStatusCode();
            Assert.True(csrfResponse.Headers.CacheControl!.NoCache);
            Assert.True(csrfResponse.Headers.CacheControl.NoStore);
            Assert.Contains(csrfResponse.Headers.Pragma, header => header.Name == "no-cache");
            var csrf = await csrfResponse.Content.ReadFromJsonAsync<JsonElement>();
            client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf.GetProperty("data").GetProperty("requestToken").GetString());
            using var login = await client.PostAsJsonAsync("/api/v1/auth/web/login", new { email = "user@example.test", password = "Password123!" });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            var result = await login.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("access-1", result.GetProperty("data").GetProperty("accessToken").GetString());
            Assert.False(result.GetProperty("data").TryGetProperty("refreshToken", out _));
            Assert.Equal("no-store", login.Headers.CacheControl!.ToString());
            Assert.Contains(login.Headers.GetValues("Set-Cookie"), cookie => cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase)
                && cookie.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase));
            using var refreshed = await client.PostAsJsonAsync("/api/v1/auth/web/refresh", new { refreshToken = "untrusted-body-token" });
            Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
            Assert.Equal("refresh-1", identity.ReceivedRefreshToken);
            Assert.Contains(refreshed.Headers.GetValues("Set-Cookie"), cookie => cookie.Contains("refresh-2", StringComparison.Ordinal));
            using var logout = await client.PostAsync("/api/v1/auth/web/logout", null);
            Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
            Assert.Equal("refresh-2", identity.LoggedOutToken);
            Assert.DoesNotContain(handler.CookieContainer.GetCookies(client.BaseAddress).Cast<Cookie>(), cookie => cookie.Name == "matchingsports.refresh");
        }
        finally { await app.StopAsync(); }
    }

    private sealed class FakeIdentity : IIdentityService
    {
        public int Logins { get; private set; }
        public string? ReceivedRefreshToken { get; private set; }
        public string? LoggedOutToken { get; private set; }
        private static AuthTokenResult Result(int number) => new($"access-{number}", $"refresh-{number}", "Bearer",
            DateTimeOffset.UtcNow.AddMinutes(5), DateTimeOffset.UtcNow.AddDays(1),
            new CurrentUserResult(Guid.NewGuid(), "user@example.test", "User", true, [], []));
        public Task<AuthTokenResult> LoginAsync(string email, string password, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
        { Logins++; return Task.FromResult(Result(1)); }
        public Task<AuthTokenResult> RefreshAsync(string refreshToken, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
        { ReceivedRefreshToken = refreshToken; return Task.FromResult(Result(2)); }
        public Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
        { LoggedOutToken = refreshToken; return Task.CompletedTask; }
        public Task<RegistrationResult> RegisterAsync(string email, string password, string displayName, string? ipAddress, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task VerifyEmailAsync(string email, string otp, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task ResendVerificationOtpAsync(string email, string? ipAddress, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task RequestPasswordResetAsync(string email, string? ipAddress, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task ResetPasswordAsync(string email, string otp, string newPassword, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<AuthTokenResult> ExternalLoginAsync(string provider, string providerKey, string email, string displayName, string? ipAddress, string? userAgent, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<CurrentUserResult> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
