using System.Security.Claims;
using MatchingSports.Application.Abstractions;
using MatchingSports.Domain.Entities;
using MatchingSports.Infrastructure;
using MatchingSports.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MatchingSports.Tests;

public sealed class RoleAuthorizationTests
{
    [Theory]
    [InlineData(RoleNames.Admin, false, false)]
    [InlineData(RoleNames.Admin, true, true)]
    [InlineData(RoleNames.User, false, false)]
    [InlineData(RoleNames.User, true, true)]
    [InlineData("ACCESS_MANAGER", false, false)]
    [InlineData("ACCESS_MANAGER", true, true)]
    public async Task PoliciesRequirePermissionRegardlessOfRole(string role, bool hasPermission, bool expected)
    {
        using var provider = CreateProvider(new StubRoleRepository(null));
        using var scope = provider.CreateScope();
        var claims = new List<Claim> { new(ClaimTypes.Role, role) };
        if (hasPermission)
            claims.Add(new Claim(AuthorizationClaimTypes.Permission, PermissionCodes.AssignUserRole));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        var result = await scope.ServiceProvider.GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(principal, null, PermissionCodes.AssignUserRole);

        Assert.Equal(expected, result.Succeeded);
    }

    [Fact]
    public async Task NewPermissionPolicyDoesNotRequireEditingSeedCatalog()
    {
        using var provider = CreateProvider(new StubRoleRepository(null));
        using var scope = provider.CreateScope();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(AuthorizationClaimTypes.Permission, "future_module.review")], "test"));
        var service = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        Assert.True((await service.AuthorizeAsync(principal, null, "future_module.review")).Succeeded);
        Assert.False((await service.AuthorizeAsync(principal, null, "future_module.delete")).Succeeded);
    }

    [Fact]
    public async Task TokenValidationReplacesStaleAdminClaimWithCurrentDatabaseRole()
    {
        var userId = Guid.NewGuid();
        using var provider = CreateProvider(new StubRoleRepository(new UserRoleAssignment(userId, [RoleNames.User], [PermissionCodes.ReadOwnProfile])));
        using var scope = provider.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        var context = CreateTokenContext(scope.ServiceProvider, options, userId);

        await options.Events.OnTokenValidated(context);

        Assert.False(context.Principal!.IsInRole(RoleNames.Admin));
        Assert.True(context.Principal.IsInRole(RoleNames.User));
        Assert.False(context.Principal.HasClaim(AuthorizationClaimTypes.Permission, PermissionCodes.AssignUserRole));
        Assert.True(context.Principal.HasClaim(AuthorizationClaimTypes.Permission, PermissionCodes.ReadOwnProfile));
    }

    [Fact]
    public async Task RevokedPermissionIsDeniedEvenWhenAdminRoleIsUnchanged()
    {
        var userId = Guid.NewGuid();
        using var provider = CreateProvider(new StubRoleRepository(new UserRoleAssignment(userId, [RoleNames.Admin], [PermissionCodes.ReadOwnProfile])));
        using var scope = provider.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        var context = CreateTokenContext(scope.ServiceProvider, options, userId);

        await options.Events.OnTokenValidated(context);
        var authorization = await scope.ServiceProvider.GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(context.Principal!, null, PermissionCodes.AssignUserRole);

        Assert.True(context.Principal!.IsInRole(RoleNames.Admin));
        Assert.False(authorization.Succeeded);
    }

    [Fact]
    public async Task TokenValidationRejectsMissingOrInactiveAccount()
    {
        using var provider = CreateProvider(new StubRoleRepository(null));
        using var scope = provider.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        var context = CreateTokenContext(scope.ServiceProvider, options, Guid.NewGuid());

        await options.Events.OnTokenValidated(context);

        Assert.NotNull(context.Result?.Failure);
    }

    private static TokenValidatedContext CreateTokenContext(IServiceProvider services, JwtBearerOptions options, Guid userId) =>
        new(new DefaultHttpContext { RequestServices = services },
            new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler)), options)
        {
            Principal = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim(ClaimTypes.Role, RoleNames.Admin),
                 new Claim(AuthorizationClaimTypes.Permission, PermissionCodes.AssignUserRole)], "test"))
        };

    private static ServiceProvider CreateProvider(IUserRoleRepository repository)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:matchingsports"] = "Host=localhost;Database=role_tests;Username=test;Password=test",
            ["Auth:Jwt:SigningKey"] = new string('x', 32),
            ["Auth:Otp:Pepper"] = new string('y', 32)
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        services.AddScoped<IUserRoleRepository>(_ => repository);
        return services.BuildServiceProvider();
    }

    private sealed class StubRoleRepository(UserRoleAssignment? assignment) : IUserRoleRepository
    {
        public Task<UserRoleAssignment?> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(assignment?.UserId == userId ? assignment : null);

        public Task ReplaceUserRolesAsync(UserRoleAssignment assignment, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
