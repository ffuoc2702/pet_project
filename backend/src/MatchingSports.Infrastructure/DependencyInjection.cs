using System.Text;
using MatchingSports.Application.Abstractions;
using MatchingSports.Domain.Entities;
using MatchingSports.Infrastructure.Identity;
using MatchingSports.Infrastructure.Persistence;
using MatchingSports.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace MatchingSports.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("matchingsports")
            ?? throw new InvalidOperationException("Connection string 'matchingsports' is not configured.");

        services.AddDbContext<MatchingSportsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite()));

        var authOptions = configuration
            .GetSection(AuthOptions.SectionName)
            .Get<AuthOptions>() ?? new AuthOptions();
        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .Validate(
                options => Encoding.UTF8.GetByteCount(options.Jwt.SigningKey) >= 32,
                "Auth:Jwt:SigningKey must contain at least 32 bytes.")
            .Validate(
                options => Encoding.UTF8.GetByteCount(options.Otp.Pepper) >= 32,
                "Auth:Otp:Pepper must contain at least 32 bytes.")
            .ValidateOnStart();

        services.AddIdentityCore<User>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<MatchingSportsDbContext>()
            .AddDefaultTokenProviders()
            .AddSignInManager();

        var authentication = services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authOptions.Jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = authOptions.Jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(authOptions.Jwt.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = System.Security.Claims.ClaimTypes.Name,
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role
                };
            })
            .AddCookie(IdentityConstants.ExternalScheme, options =>
            {
                options.Cookie.Name = "matchingsports.external";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            });

        if (!string.IsNullOrWhiteSpace(authOptions.Google.ClientId)
            && !string.IsNullOrWhiteSpace(authOptions.Google.ClientSecret))
        {
            authentication.AddGoogle("Google", options =>
            {
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.ClientId = authOptions.Google.ClientId;
                options.ClientSecret = authOptions.Google.ClientSecret;
                options.CallbackPath = authOptions.Google.CallbackPath;
                options.SaveTokens = false;
            });
        }

        services.AddAuthorization();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IOtpEmailSender, GmailOtpEmailSender>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ISportRepository, SportRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PersistenceExceptionBehavior<,>));

        return services;
    }
}
