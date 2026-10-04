using System.Security.Claims;
using MatchingSports.Api.Identity;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace MatchingSports.Tests;

public sealed class CurrentUserTests
{
    [Fact]
    public void ReadsAuthenticatedUserAndUsesScopedLifetime()
    {
        var services = new ServiceCollection();
        services.AddCurrentUser();
        using var provider = services.BuildServiceProvider();
        var accessor = provider.GetRequiredService<IHttpContextAccessor>();
        var userId = Guid.NewGuid();
        accessor.HttpContext = CreateContext(userId.ToString(), true);

        try
        {
            using var firstScope = provider.CreateScope();
            using var secondScope = provider.CreateScope();
            var currentUser = firstScope.ServiceProvider.GetRequiredService<ICurrentUser>();

            Assert.Equal(userId, currentUser.UserId);
            Assert.Same(currentUser, firstScope.ServiceProvider.GetRequiredService<ICurrentUser>());
            Assert.NotSame(currentUser, secondScope.ServiceProvider.GetRequiredService<ICurrentUser>());
        }
        finally
        {
            accessor.HttpContext = null;
        }
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("invalid-guid", true)]
    [InlineData("771de91f-b105-4d71-9d76-c0c253d34d26", false)]
    public void RejectsMissingOrInvalidIdentity(string? claimValue, bool isAuthenticated)
    {
        AssertInvalidAccessToken(CreateContext(claimValue, isAuthenticated));
    }

    [Fact]
    public void RejectsMissingHttpContext() => AssertInvalidAccessToken(null);

    private static void AssertInvalidAccessToken(HttpContext? context)
    {
        var services = new ServiceCollection();
        services.AddCurrentUser();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var accessor = provider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = context;

        try
        {
            var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();
            var exception = Assert.Throws<UnauthorizedException>(() => currentUser.UserId);
            Assert.Equal(ErrorCodes.InvalidAccessToken, exception.Code);
        }
        finally
        {
            accessor.HttpContext = null;
        }
    }

    private static DefaultHttpContext CreateContext(string? claimValue, bool isAuthenticated)
    {
        Claim[] claims = claimValue is null ? [] : [new Claim(ClaimTypes.NameIdentifier, claimValue)];
        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, isAuthenticated ? "Bearer" : null))
        };
    }
}
