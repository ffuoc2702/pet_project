using Microsoft.AspNetCore.Antiforgery;

namespace MatchingSports.Api.Identity;

public static class WebAuthenticationRegistration
{
    public static IServiceCollection AddWebAuthentication(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = environment.IsDevelopment() ? "matchingsports.csrf" : "__Host-matchingsports.csrf";
            options.Cookie.HttpOnly = true;
            options.Cookie.Path = "/";
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        });
        return services;
    }
}
