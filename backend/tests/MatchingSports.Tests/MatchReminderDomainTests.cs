using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Domain.ValueObjects;
using MatchingSports.Shared;

namespace MatchingSports.Tests;

public sealed class MatchReminderDomainTests
{
    private static readonly DateTimeOffset StartAt = new(2026, 10, 9, 15, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(MatchStatus.PENDING)]
    [InlineData(MatchStatus.CANCELLED)]
    [InlineData(MatchStatus.COMPLETED)]
    public void OnlyConfirmedMatchesCanBeReminded(MatchStatus status)
    {
        var match = new Match { Status = status, StartAt = StartAt };
        Assert.False(match.IsReminderDue(StartAt.AddMinutes(-30), TimeSpan.FromHours(1)));
        Assert.Throws<MatchingRuleException>(() => match.RecordReminderPublished(StartAt.AddMinutes(-30), TimeSpan.FromHours(1)));
        Assert.Null(match.ReminderPublishedAt);
    }

    [Fact]
    public void ReminderWindowIncludesThresholdExcludesKickoffAndCannotRepeat()
    {
        var match = new Match { Status = MatchStatus.CONFIRMED, StartAt = StartAt };
        var lead = TimeSpan.FromHours(1);
        Assert.False(match.IsReminderDue(StartAt.AddMinutes(-61), lead));
        Assert.True(match.IsReminderDue(StartAt.AddHours(-1), lead));
        Assert.True(match.IsReminderDue(StartAt.AddTicks(-1), lead));
        Assert.False(match.IsReminderDue(StartAt, lead));
        Assert.False(match.IsReminderDue(StartAt.AddSeconds(1), lead));
        var token = match.ConcurrencyToken;
        match.RecordReminderPublished(StartAt.AddHours(-1), lead);
        Assert.Equal(StartAt.AddHours(-1), match.ReminderPublishedAt);
        Assert.NotEqual(token, match.ConcurrencyToken);
        Assert.False(match.IsReminderDue(StartAt.AddMinutes(-30), lead));
        Assert.Throws<MatchingRuleException>(() => match.RecordReminderPublished(StartAt.AddMinutes(-30), lead));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10081)]
    public void InvalidReminderSettingDoesNotPartiallyChangeSettings(int minutes)
    {
        var settings = SystemSettings.CreateDefaults();
        var before = settings.GetValues();
        Assert.Throws<ArgumentOutOfRangeException>(() => settings.UpdateValues(before with { MatchReminderLeadMinutes = minutes }));
        Assert.Equal(before, settings.GetValues());
    }

    [Fact]
    public void ReminderRequiresMatchLinkAndFutureDeliveryExpiry()
    {
        Assert.Throws<MatchingRuleException>(() => NotificationHistory.CreateMatchingEvent(Guid.NewGuid(), EntityTypes.Match,
            Guid.NewGuid(), NotificationType.MATCH_REMINDER, "Nhắc lịch", "Sắp bắt đầu", "{\"SchemaVersion\":1}", StartAt, true));
        var history = NotificationHistory.CreateMatchingEvent(Guid.NewGuid(), EntityTypes.Match, Guid.NewGuid(),
            NotificationType.MATCH_REMINDER, "Nhắc lịch", "Sắp bắt đầu", "{\"SchemaVersion\":1}", StartAt.AddMinutes(-30), true, StartAt);
        Assert.Equal(StartAt, history.ExpiresAt);
    }
}
