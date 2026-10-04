using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Infrastructure;
using MatchingSports.Infrastructure.Persistence.Seed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MatchingSports.Tests;

public sealed class AdminAccountSeedTests
{
    [Fact]
    public void VerifiedAccountFactoryNormalizesEmailAndUsesDomainProfileUpdate()
    {
        var user = User.CreateVerifiedAccount("  Admin@Example.test  ", "  Quản trị viên  ");
        Assert.Equal("admin@example.test", user.Email);
        Assert.Equal(user.Email, user.UserName);
        Assert.True(user.EmailConfirmed);
        Assert.Equal(UserStatus.ACTIVE, user.Status);
        Assert.Equal("Quản trị viên", user.Profile!.DisplayName);
        Assert.Equal(user.Id, user.Profile.UserId);
        Assert.True(user.CanReceiveInitialAdministratorRole());
    }

    [Theory]
    [InlineData(UserStatus.SUSPENDED, true, false)]
    [InlineData(UserStatus.ACTIVE, false, false)]
    [InlineData(UserStatus.ACTIVE, true, true)]
    public void BootstrapEligibilityDoesNotBypassAccountState(UserStatus status, bool verified, bool deleted)
    {
        var user = User.CreateVerifiedAccount("admin@example.test", "Admin");
        user.Status = status;
        user.EmailConfirmed = verified;
        user.DeletedAt = deleted ? DateTimeOffset.UtcNow : null;
        Assert.False(user.CanReceiveInitialAdministratorRole());
    }

    [Theory]
    [InlineData("", "Testing-Password123!")]
    [InlineData("not-an-email", "Testing-Password123!")]
    [InlineData("admin@example.test", "")]
    public void EnabledAdminSeedRequiresEmailAndSecretWithoutLeakingValues(string email, string password)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:matchingsports"] = "Host=localhost;Database=options_validation;Username=postgres",
            ["Seed:Admin:Enabled"] = "true",
            ["Seed:Admin:Email"] = email,
            ["Seed:Admin:Password"] = password
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();
        var error = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<SeedAdminAccountOptions>>().Value);
        Assert.DoesNotContain("Testing-Password123!", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AdminAccountSeedIsDisabledUnlessExplicitlyEnabled()
    {
        var options = new SeedAdminAccountOptions();
        Assert.False(options.Enabled);
        Assert.Empty(options.Password);
        Assert.Empty(options.Email);
    }
}
