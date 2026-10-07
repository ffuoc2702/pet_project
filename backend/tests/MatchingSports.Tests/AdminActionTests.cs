using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;

namespace MatchingSports.Tests;

public sealed class AdminActionTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MissingActorOrTargetIsRejected(bool missingActor)
    {
        Assert.Throws<ArgumentException>(() => AdminAction.Create(
            missingActor ? Guid.Empty : Guid.NewGuid(), AdminActionType.CREATE_ROLE, "ROLE",
            missingActor ? Guid.NewGuid() : Guid.Empty, "null", "{}"));
    }

    [Fact]
    public void UndefinedActionTypeIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AdminAction.Create(
            Guid.NewGuid(), (AdminActionType)999, "ROLE", Guid.NewGuid(), "null", "{}"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1234567890123456789012345678901")]
    public void InvalidTargetTypeIsRejected(string targetType)
    {
        Assert.Throws<ArgumentException>(() => AdminAction.Create(
            Guid.NewGuid(), AdminActionType.CREATE_ROLE, targetType, Guid.NewGuid(), "null", "{}"));
    }

    [Fact]
    public void ModerationActionRetainsContextAndNormalizesReason()
    {
        var correlationId = Guid.NewGuid();
        var action = AdminAction.Create(Guid.NewGuid(), AdminActionType.SUSPEND_USER, " USER ",
            Guid.NewGuid(), "{\"status\":\"ACTIVE\"}", "{\"status\":\"SUSPENDED\"}",
            "  Report confirmed  ", "{\"report\":true}", correlationId);

        Assert.Equal("USER", action.TargetType);
        Assert.Equal("Report confirmed", action.Reason);
        Assert.Equal(correlationId, action.CorrelationId);
        Assert.Equal("{\"report\":true}", action.MetadataJson);
    }
}
