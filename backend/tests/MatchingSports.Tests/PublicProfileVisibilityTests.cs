using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Domain.Services;

namespace MatchingSports.Tests;

public sealed class PublicProfileVisibilityTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void UserWithoutPubliclyVisibleAccountCannotExposeProfile(int hiddenCase)
    {
        var user = User.CreateVerifiedAccount("player@example.test", "Player");
        Assert.True(PublicProfileVisibility.Users.Compile()(user));
        switch (hiddenCase)
        {
            case 0: user.EmailConfirmed = false; break;
            case 1: user.Status = UserStatus.SUSPENDED; break;
            case 2: user.DeletedAt = DateTimeOffset.UtcNow; break;
            default: user.Profile = null; break;
        }
        Assert.False(PublicProfileVisibility.Users.Compile()(user));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void TeamVisibilityRequiresActiveTeamSportAndVisibleCaptain(int hiddenCase)
    {
        var team = new Team
        {
            Status = TeamStatus.ACTIVE,
            Sport = new Sport { IsActive = true },
            CaptainUser = User.CreateVerifiedAccount("captain@example.test", "Captain")
        };
        Assert.True(PublicProfileVisibility.Teams.Compile()(team));
        switch (hiddenCase)
        {
            case 0: team.Status = TeamStatus.INACTIVE; break;
            case 1: team.DeletedAt = DateTimeOffset.UtcNow; break;
            case 2: team.Sport.IsActive = false; break;
            default: team.CaptainUser.Status = UserStatus.SUSPENDED; break;
        }
        Assert.False(PublicProfileVisibility.Teams.Compile()(team));
    }
}
