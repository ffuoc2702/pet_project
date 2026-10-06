using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using MatchingSports.Api.Contracts;
using MatchingSports.Api.Controllers;
using MatchingSports.Api.ErrorHandling;
using MatchingSports.Api.Identity;
using MatchingSports.Application;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Identity;
using MatchingSports.Application.Features.UserSportProfile;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Infrastructure;
using MatchingSports.Infrastructure.Persistence;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MatchingSports.Tests;

public sealed class UserSportProfileTests
{
    [Fact]
    public async Task HttpCrudAndAuthMeUseAuthenticatedIdentityAndReturnSportProfileDetails()
    {
        var store = new Store();
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddApplication();
        builder.Services.AddSingleton<IUserSportProfileRepository>(store);
        builder.Services.AddSingleton<IUserRoleRepository>(store);
        builder.Services.AddSingleton<IUnitOfWork>(store);
        builder.Services.AddSingleton<IIdentityService>(store);
        builder.Services.AddCurrentUser();
        builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(PermissionCodes.UpdateSportProfiles, policy => policy.RequireAuthenticatedUser());
            options.AddPolicy(PermissionCodes.ReadOwnProfile, policy => policy.RequireAuthenticatedUser());
        });
        builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("AuthStrict", limiter =>
        {
            limiter.PermitLimit = 100;
            limiter.Window = TimeSpan.FromMinutes(1);
        }));
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddControllers().AddApplicationPart(typeof(UserSportProfileController).Assembly)
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        await using var app = builder.Build();
        store.Mapper = app.Services.GetRequiredService<IMapper>();
        app.Urls.Add("http://127.0.0.1:0");
        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        app.MapControllers();
        await app.StartAsync();
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            var request = new { sportId = store.Sport.Id, level = "BEGINNER", preferredFormatIds = new[] { store.Sport.Formats.First().Id }, bio = "  hello  " };
            using var anonymous = await client.PostAsJsonAsync("/api/v1/user-sport-profiles", request);
            Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
            client.DefaultRequestHeaders.Add("X-Test-User", store.UserId.ToString());
            using var created = await client.PostAsJsonAsync("/api/v1/user-sport-profiles", request);
            Assert.Equal(HttpStatusCode.OK, created.StatusCode);
            using var createdJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
            var id = createdJson.RootElement.GetProperty("data").GetProperty("id").GetGuid();

            using var duplicate = await client.PostAsJsonAsync("/api/v1/user-sport-profiles", request);
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
            using var invalid = await client.PutAsJsonAsync($"/api/v1/user-sport-profiles/{id}",
                new { level = "EXPERT", preferredFormatIds = Array.Empty<Guid>() });
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            using var updated = await client.PutAsJsonAsync($"/api/v1/user-sport-profiles/{id}",
                new { level = "ADVANCED", preferredFormatIds = new[] { store.Sport.Formats.Last().Id }, bio = "updated" });
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

            using var me = await client.GetAsync("/api/v1/auth/me");
            Assert.Equal(HttpStatusCode.OK, me.StatusCode);
            using var meJson = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
            var profileJson = Assert.Single(meJson.RootElement.GetProperty("data").GetProperty("sportProfiles").EnumerateArray());
            Assert.Equal(id, profileJson.GetProperty("id").GetGuid());
            Assert.Equal("ADVANCED", profileJson.GetProperty("level").GetString());
            Assert.Equal("Tennis", profileJson.GetProperty("sportName").GetString());
            Assert.Equal("DOUBLES", Assert.Single(profileJson.GetProperty("preferredFormats").EnumerateArray()).GetProperty("code").GetString());

            var other = UserSportProfile.Create(Guid.NewGuid(), store.Sport, "BEGINNER", [], null);
            store.Profiles.Add(other);
            using var forbiddenRecord = await client.DeleteAsync($"/api/v1/user-sport-profiles/{other.Id}");
            Assert.Equal(HttpStatusCode.NotFound, forbiddenRecord.StatusCode);
            using var deleted = await client.DeleteAsync($"/api/v1/user-sport-profiles/{id}");
            Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
            using var emptyMe = await client.GetAsync("/api/v1/auth/me");
            using var emptyJson = JsonDocument.Parse(await emptyMe.Content.ReadAsStringAsync());
            Assert.Empty(emptyJson.RootElement.GetProperty("data").GetProperty("sportProfiles").EnumerateArray());
            Assert.Same(other, Assert.Single(store.Profiles));
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public void DomainNormalizesBioAndReplacesPreferredFormats()
    {
        var sport = CreateSport();
        var first = sport.Formats.First();
        var second = sport.Formats.Last();
        var profile = UserSportProfile.Create(Guid.NewGuid(), sport, "BEGINNER", [first.Id], "  giới thiệu  ");
        Assert.Equal("giới thiệu", profile.Bio);

        profile.UpdateDetails("ADVANCED", [second.Id], "   ");

        Assert.Equal(SportLevel.ADVANCED, profile.Level);
        Assert.Null(profile.Bio);
        Assert.Equal(second.Id, Assert.Single(profile.PreferredFormats).Id);
        profile.UpdateDetails("INTERMEDIATE", [], null);
        Assert.Empty(profile.PreferredFormats);
    }

    [Theory]
    [InlineData("level")]
    [InlineData("numeric-level")]
    [InlineData("duplicate")]
    [InlineData("unknown")]
    [InlineData("empty-id")]
    [InlineData("other-sport")]
    [InlineData("inactive-format")]
    [InlineData("inactive-sport")]
    [InlineData("null-formats")]
    public void InvalidUpdateDoesNotPartiallyChangeProfile(string invalidCase)
    {
        var sport = CreateSport();
        var original = sport.Formats.First();
        var selected = sport.Formats.Last();
        var profile = UserSportProfile.Create(Guid.NewGuid(), sport, "BEGINNER", [original.Id], "old");
        IReadOnlyCollection<Guid> ids = [selected.Id];
        var level = "ADVANCED";
        switch (invalidCase)
        {
            case "level": level = "EXPERT"; break;
            case "numeric-level": level = "0"; break;
            case "duplicate": ids = [selected.Id, selected.Id]; break;
            case "unknown": ids = [Guid.NewGuid()]; break;
            case "empty-id": ids = [Guid.Empty]; break;
            case "other-sport": selected.SportId = Guid.NewGuid(); break;
            case "inactive-format": selected.Deactivate(); break;
            case "inactive-sport": sport.IsActive = false; break;
            case "null-formats": ids = null!; break;
        }

        Assert.ThrowsAny<ArgumentException>(() => profile.UpdateDetails(level, ids, "new"));
        Assert.Equal(SportLevel.BEGINNER, profile.Level);
        Assert.Equal("old", profile.Bio);
        Assert.Same(original, Assert.Single(profile.PreferredFormats));
    }

    [Fact]
    public async Task MediatorCreatesUpdatesReadsMeAndDeletesOnlyTheSelectedRecord()
    {
        var store = new Store();
        using var provider = CreateProvider(store);
        var mediator = provider.GetRequiredService<IMediator>();
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var created = await mediator.Send(new CreateUserSportProfileCommand(store.UserId, store.Sport.Id,
            "BEGINNER", [store.Sport.Formats.First().Id], "  hello  "), token);
        Assert.Equal("hello", created.Bio);
        Assert.Equal(store.Sport.Name, created.SportName);
        Assert.Equal(store.UserId, Assert.Single(store.Profiles).UserId);

        var updated = await mediator.Send(new UpdateUserSportProfileCommand(store.UserId, created.Id,
            "ADVANCED", [store.Sport.Formats.Last().Id], null), token);
        Assert.Equal(SportLevel.ADVANCED, updated.Level);
        Assert.Equal(store.Sport.Formats.Last().Id, Assert.Single(updated.PreferredFormats).Id);
        Assert.Equal(1, store.UpdateCount);
        Assert.Equal(2, store.SaveCount);

        var me = await mediator.Send(new GetCurrentUserQuery { ActorUserId = store.UserId }, token);
        var response = CurrentUserResponse.From(me);
        Assert.Equal(created.Id, Assert.Single(response.SportProfiles).Id);
        Assert.Equal("DOUBLES", Assert.Single(response.SportProfiles.Single().PreferredFormats).Code);
        Assert.Equal(store.IdentityResult.Email, response.Email);
        Assert.Equal(2, store.SaveCount);

        var other = UserSportProfile.Create(store.UserId, CreateSport(), "BEGINNER", [], null);
        store.Profiles.Add(other);
        await mediator.Send(new DeleteUserSportProfileCommand(store.UserId, created.Id), token);
        Assert.Same(other, Assert.Single(store.Profiles));
        Assert.Equal(3, store.SaveCount);
        Assert.All(store.Tokens, actual => Assert.Equal(token, actual));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task PermissionPipelineRejectsEveryUseCaseBeforeReadingOrSaving(int operation)
    {
        var store = new Store { Granted = false };
        using var provider = CreateProvider(store);
        var mediator = provider.GetRequiredService<IMediator>();
        var exception = await Assert.ThrowsAsync<ForbiddenException>(() => SendAsync(mediator, store, operation));
        Assert.Equal(ErrorCodes.Forbidden, exception.Code);
        Assert.Equal(0, store.ReadCount);
        Assert.Equal(0, store.SaveCount);
        Assert.Equal(1, store.AuthorizationCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateAndDeleteRejectAnotherUsersRecord(bool delete)
    {
        var store = new Store();
        var otherProfile = UserSportProfile.Create(Guid.NewGuid(), store.Sport, "BEGINNER", [], "private");
        store.Profiles.Add(otherProfile);
        using var provider = CreateProvider(store);
        var mediator = provider.GetRequiredService<IMediator>();
        var exception = await Assert.ThrowsAsync<NotFoundException>(async () =>
        {
            if (delete)
                await mediator.Send(new DeleteUserSportProfileCommand(store.UserId, otherProfile.Id));
            else
                await mediator.Send(new UpdateUserSportProfileCommand(store.UserId, otherProfile.Id, "ADVANCED", [], "new"));
        });
        Assert.Equal(ErrorCodes.UserSportProfileNotFound, exception.Code);
        Assert.Equal("private", otherProfile.Bio);
        Assert.Equal(0, store.SaveCount);
        Assert.Same(otherProfile, Assert.Single(store.Profiles));
    }

    [Fact]
    public async Task DuplicateSportProfileIsRejectedWithoutCommit()
    {
        var store = new Store();
        store.Profiles.Add(UserSportProfile.Create(store.UserId, store.Sport, "BEGINNER", [], null));
        using var provider = CreateProvider(store);
        var exception = await Assert.ThrowsAsync<ConflictException>(() => SendAsync(provider.GetRequiredService<IMediator>(), store, 0));
        Assert.Equal(ErrorCodes.DuplicateResource, exception.Code);
        Assert.Equal(0, store.SaveCount);
        Assert.Single(store.Profiles);
    }

    [Fact]
    public async Task InvalidFormatSelectionReturnsStableValidationErrorWithoutCommit()
    {
        var store = new Store();
        using var provider = CreateProvider(store);
        var exception = await Assert.ThrowsAsync<RequestValidationException>(() => provider.GetRequiredService<IMediator>().Send(
            new CreateUserSportProfileCommand(store.UserId, store.Sport.Id, "BEGINNER", [Guid.NewGuid()], null)));
        Assert.Equal(ErrorCodes.InvalidUserSportProfile, exception.Code);
        Assert.Equal(0, store.SaveCount);
        Assert.Empty(store.Profiles);
    }

    [Fact]
    public async Task MeWithoutSportProfilesReturnsEmptyCollectionAndDoesNotCommit()
    {
        var store = new Store();
        using var provider = CreateProvider(store);
        var me = await provider.GetRequiredService<IMediator>().Send(new GetCurrentUserQuery { ActorUserId = store.UserId });
        Assert.Empty(CurrentUserResponse.From(me).SportProfiles);
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public void FormatOnlyUpdateTracksLinkChangesAndUpdatesProfileTimestamp()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:matchingsports"] = "Host=localhost;Database=tracking_validation;Username=postgres"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IUserSportProfileRepository>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var sport = CreateSport();
        var profile = UserSportProfile.Create(Guid.NewGuid(), sport, "BEGINNER", [sport.Formats.First().Id], null);
        db.Attach(profile);

        profile.UpdateDetails("BEGINNER", [sport.Formats.Last().Id], null);
        repository.Update(profile);
        db.ChangeTracker.DetectChanges();

        Assert.True(db.Entry(profile).Property(item => item.UpdatedAt).IsModified);
        Assert.Single(db.ChangeTracker.Entries<UserSportProfileFormat>(), entry => entry.State == EntityState.Deleted);
        Assert.Single(db.ChangeTracker.Entries<UserSportProfileFormat>(), entry => entry.State == EntityState.Added);
        Assert.All(db.ChangeTracker.Entries<SportFormat>(), entry => Assert.Equal(EntityState.Unchanged, entry.State));
        // Verify EF can translate the nested DTO projection used by auth/me without executing I/O.
        var sql = db.UserSportProfiles.AsNoTracking().Where(item => item.UserId == profile.UserId)
            .ProjectTo<UserSportProfileDto>(mapper.ConfigurationProvider).ToQueryString();
        Assert.Contains("user_sport_profile_formats", sql, StringComparison.Ordinal);
        Assert.Contains("sport_formats", sql, StringComparison.Ordinal);
    }

    private static async Task SendAsync(IMediator mediator, Store store, int operation)
    {
        switch (operation)
        {
            case 0: await mediator.Send(new CreateUserSportProfileCommand(store.UserId, store.Sport.Id, "BEGINNER", [], null)); break;
            case 1: await mediator.Send(new UpdateUserSportProfileCommand(store.UserId, Guid.NewGuid(), "BEGINNER", [], null)); break;
            case 2: await mediator.Send(new DeleteUserSportProfileCommand(store.UserId, Guid.NewGuid())); break;
            default: await mediator.Send(new GetCurrentUserQuery { ActorUserId = store.UserId }); break;
        }
    }

    private static ServiceProvider CreateProvider(Store store)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<ICurrentUser>(store);
        services.AddSingleton<IUserSportProfileRepository>(store);
        services.AddSingleton<IUserRoleRepository>(store);
        services.AddSingleton<IUnitOfWork>(store);
        services.AddSingleton<IIdentityService>(store);
        var provider = services.BuildServiceProvider();
        store.Mapper = provider.GetRequiredService<IMapper>();
        return provider;
    }

    private static Sport CreateSport()
    {
        var sport = new Sport { Id = Guid.NewGuid(), Code = "TENNIS", Name = "Tennis", IsActive = true };
        sport.Formats.Add(new SportFormat { Id = Guid.NewGuid(), SportId = sport.Id, Code = "SINGLES", DisplayName = "Singles", SideSize = 1 });
        sport.Formats.Add(new SportFormat { Id = Guid.NewGuid(), SportId = sport.Id, Code = "DOUBLES", DisplayName = "Doubles", SideSize = 2 });
        return sport;
    }

    private sealed class TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Guid.TryParse(Request.Headers["X-Test-User"], out var userId))
                return Task.FromResult(AuthenticateResult.NoResult());
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Test");
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), "Test")));
        }
    }

    private sealed class Store : IUserSportProfileRepository, ICurrentUser, IUserRoleRepository, IUnitOfWork, IIdentityService
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public Sport Sport { get; } = CreateSport();
        public List<UserSportProfile> Profiles { get; } = [];
        public List<CancellationToken> Tokens { get; } = [];
        public IMapper Mapper { get; set; } = null!;
        public bool Granted { get; init; } = true;
        public int ReadCount { get; private set; }
        public int SaveCount { get; private set; }
        public int UpdateCount { get; private set; }
        public int AuthorizationCount { get; private set; }
        public CurrentUserResult IdentityResult => new(UserId, "user@example.test", "Test user", true, ["USER"], []);

        private void Read(CancellationToken cancellationToken)
        {
            ReadCount++;
            Tokens.Add(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        }

        public Task<UserSportProfile?> GetForUpdateAsync(Guid id, Guid userId, CancellationToken cancellationToken)
        {
            Read(cancellationToken);
            return Task.FromResult(Profiles.SingleOrDefault(profile => profile.Id == id && profile.UserId == userId));
        }
        public Task<bool> ExistsAsync(Guid userId, Guid sportId, CancellationToken cancellationToken)
        {
            Read(cancellationToken);
            return Task.FromResult(Profiles.Any(profile => profile.UserId == userId && profile.SportId == sportId));
        }
        public Task<Sport?> GetSportAsync(Guid sportId, CancellationToken cancellationToken)
        {
            Read(cancellationToken);
            return Task.FromResult<Sport?>(Sport.Id == sportId ? Sport : null);
        }
        public Task<IReadOnlyCollection<UserSportProfileDto>> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            Read(cancellationToken);
            return Task.FromResult<IReadOnlyCollection<UserSportProfileDto>>(Profiles.Where(profile => profile.UserId == userId)
                .Select(Mapper.Map<UserSportProfileDto>).ToArray());
        }
        public void Add(UserSportProfile profile) => Profiles.Add(profile);
        public void Update(UserSportProfile profile) => UpdateCount++;
        public void Remove(UserSportProfile profile) => Profiles.Remove(profile);
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            Tokens.Add(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            SaveCount++;
            return Task.FromResult(1);
        }
        public Task<UserRoleAssignment?> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken)
        {
            AuthorizationCount++;
            Tokens.Add(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<UserRoleAssignment?>(new(userId, ["USER"], Granted
                ? [PermissionCodes.UpdateSportProfiles, PermissionCodes.ReadOwnProfile] : []));
        }
        public Task<CurrentUserResult> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            Read(cancellationToken);
            Assert.Equal(UserId, userId);
            return Task.FromResult(IdentityResult);
        }

        public Task ReplaceUserRolesAsync(UserRoleAssignment assignment, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<RegistrationResult> RegisterAsync(string email, string password, string displayName, string? ipAddress, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task VerifyEmailAsync(string email, string otp, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task ResendVerificationOtpAsync(string email, string? ipAddress, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<AuthTokenResult> LoginAsync(string email, string password, string? ipAddress, string? userAgent, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<AuthTokenResult> RefreshAsync(string refreshToken, string? ipAddress, string? userAgent, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task LogoutAsync(string refreshToken, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task RequestPasswordResetAsync(string email, string? ipAddress, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task ResetPasswordAsync(string email, string otp, string newPassword, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<AuthTokenResult> ExternalLoginAsync(string provider, string providerKey, string email, string displayName, string? ipAddress, string? userAgent, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
