using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Domain.ValueObjects;
using MatchingSports.Infrastructure.Notifications;

namespace MatchingSports.Tests;

public sealed class NotificationDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 7, 3, 0, 0, TimeSpan.Zero);
    private static readonly CronNotificationSchedule Schedule = new();

    internal static NotificationConfigValues Values(Guid userId) => new(true, NotificationScheduleType.ONCE, null,
        NotificationCritical.INFO, "Title", "<p>Body</p>", "Body", Now, null, 1,
        NotificationAudienceType.SELECTED_USERS, [userId], [], true, false, false, false,
        "{\"SchemaVersion\":1,\"ActionType\":\"NONE\"}", "Asia/Ho_Chi_Minh");

    [Fact]
    public void DraftDoesNotSendAndOnceCompletesAfterOneOccurrence()
    {
        var userId = Guid.NewGuid();
        var config = NotificationConfig.Create(Values(userId), Schedule);
        Assert.Null(config.NextRunAt);
        Assert.Equal(NotificationConfigStatus.DRAFT, config.Status);
        Assert.False(config.CanSchedule(Now));
        config.Activate(Schedule, Now);
        var token = config.Claim(Now, TimeSpan.FromMinutes(5));
        var history = NotificationHistory.Create(config, userId, []);
        config.CompleteOccurrence(Schedule, Now);
        Assert.Equal(1, config.SendCount);
        Assert.Equal(1, history.SendNumber);
        Assert.Equal(NotificationSchedulingStatus.COMPLETED, config.SchedulingStatus);
        Assert.Null(config.NextRunAt);
        Assert.False(config.OwnsLease(token, Now));
        Assert.Throws<InvalidOperationException>(() => config.Activate(Schedule, Now));
        Assert.Throws<InvalidOperationException>(() => config.Update(Values(userId), Schedule, Now));
    }

    [Fact]
    public void CronImmediateCountsAsFirstAndMissedOccurrencesDoNotCreateABacklog()
    {
        var config = NotificationConfig.Create(Values(Guid.NewGuid()) with
        { ScheduleType = NotificationScheduleType.SCHEDULED, CronString = "0 18 * * *", MaxSendTimes = 3 }, Schedule);
        config.Activate(Schedule, Now);
        config.Claim(Now, TimeSpan.FromMinutes(5));
        config.CompleteOccurrence(Schedule, Now);
        Assert.Equal(Now.Date.AddHours(11), config.NextRunAt!.Value.UtcDateTime);
        var late = Now.AddDays(3);
        config.Claim(late, TimeSpan.FromMinutes(5));
        config.CompleteOccurrence(Schedule, late);
        Assert.Equal(2, config.SendCount);
        Assert.True(config.NextRunAt > late);
        var next = config.NextRunAt!.Value;
        config.Claim(next, TimeSpan.FromMinutes(5));
        config.CompleteOccurrence(Schedule, next);
        Assert.Equal(3, config.SendCount);
        Assert.Null(config.NextRunAt);
    }

    [Fact]
    public void ExpiredLeasesCanBeReclaimedAndOldTokensCannotOwnThem()
    {
        var config = NotificationConfig.Create(Values(Guid.NewGuid()), Schedule);
        config.Activate(Schedule, Now);
        var first = config.Claim(Now, TimeSpan.FromMinutes(5));
        Assert.Throws<InvalidOperationException>(() => config.Claim(Now, TimeSpan.FromMinutes(5)));
        var second = config.Claim(Now.AddMinutes(6), TimeSpan.FromMinutes(5));
        Assert.False(config.OwnsLease(first, Now.AddMinutes(6)));
        Assert.True(config.OwnsLease(second, Now.AddMinutes(6)));
        config.Cancel();
        Assert.False(config.OwnsLease(second, Now.AddMinutes(6)));
        Assert.Equal(NotificationSchedulingStatus.ABORTED, config.SchedulingStatus);
    }

    [Fact]
    public void UpdatesKeepExistingHistoryAndValidateBeforeChangingTheAggregate()
    {
        var userId = Guid.NewGuid();
        var values = Values(userId) with { ScheduleType = NotificationScheduleType.SCHEDULED, CronString = "* * * * *", MaxSendTimes = 2 };
        var config = NotificationConfig.Create(values, Schedule);
        config.Activate(Schedule, Now);
        config.Claim(Now, TimeSpan.FromMinutes(5));
        var history = NotificationHistory.Create(config, userId, [Guid.NewGuid(), Guid.NewGuid()]);
        Assert.Throws<InvalidOperationException>(() => config.Update(values, Schedule, Now));
        config.CompleteOccurrence(Schedule, Now);
        Assert.Throws<ArgumentException>(() => config.Update(values with { Title = "Invalid", Metadata = "[]" }, Schedule, Now));
        Assert.Equal("Title", config.Title);
        config.Update(values with { Title = "New title" }, Schedule, Now);
        Assert.Equal("Title", history.Title);
        Assert.Null(history.TeamId);
        Assert.Contains("SourceTeamIds", history.PayloadJson);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void InvalidConfigurationIsRejected(int scenario)
    {
        var values = Values(Guid.NewGuid());
        values = scenario switch
        {
            0 => values with { InAppEnabled = false },
            1 => values with { MaxSendTimes = 2 },
            2 => values with { ExpiredAfter = Now },
            3 => values with { AudienceType = NotificationAudienceType.ALL_USERS },
            4 => values with { CronString = "* * * * *" },
            5 => values with { Metadata = "{\"SchemaVersion\":1,\"EntityId\":{}}" },
            _ => values with { ScheduleType = NotificationScheduleType.SCHEDULED, CronString = "invalid", MaxSendTimes = null }
        };
        Assert.Throws<ArgumentException>(() => NotificationConfig.Create(values, Schedule));
    }

    [Fact]
    public void ReadingDoesNotMarkEmailSentAndKeepsFirstReadTime()
    {
        var config = NotificationConfig.Create(Values(Guid.NewGuid()) with { EmailEnabled = true }, Schedule);
        config.Activate(Schedule, Now);
        config.Claim(Now, TimeSpan.FromMinutes(5));
        var history = NotificationHistory.Create(config, Guid.NewGuid(), []);
        history.MarkRead(Now);
        history.MarkRead(Now.AddMinutes(1));
        Assert.Equal(Now, history.ReadAt);
        Assert.Equal(NotificationDeliveryStatus.PENDING, history.EmailStatus);
        history.RecordDelivery(NotificationChannel.EMAIL, NotificationDeliveryStatus.SENT, Now.AddMinutes(2));
        Assert.Equal(Now, history.ReadAt);
        history.RecordDelivery(NotificationChannel.EMAIL, NotificationDeliveryStatus.FAILED, Now.AddMinutes(3), "ERROR");
        Assert.Equal(NotificationDeliveryStatus.SENT, history.EmailStatus);
    }

    [Fact]
    public void HtmlSanitizationRemovesExecutableMarkupAndProvidesMobileText()
    {
        var content = new NotificationContentRenderer().Render("<p onclick='bad()'>Hello <b>mobile</b></p><script>bad()</script><a href='javascript:bad()'>Link</a><img src=x onerror=bad()>");
        Assert.DoesNotContain("<script", content.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", content.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", content.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<img", content.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Hello mobile", content.Text);
        Assert.DoesNotContain("<b>", content.Text);
    }

    [Fact]
    public void ExpirationIsExclusiveAndOnceMustHaveAFutureTimeWhenNotImmediate()
    {
        var values = Values(Guid.NewGuid()) with { IsSendNow = false, AvailableAt = Now.AddHours(1), ExpiredAfter = Now.AddHours(2) };
        var config = NotificationConfig.Create(values, Schedule);
        config.Activate(Schedule, Now);
        Assert.Equal(values.AvailableAt, config.NextRunAt);
        Assert.True(config.HasExpired(values.ExpiredAfter!.Value));
        var expired = NotificationConfig.Create(values, Schedule);
        Assert.Throws<ArgumentException>(() => expired.Activate(Schedule, values.ExpiredAfter!.Value));
        var past = NotificationConfig.Create(values with { AvailableAt = Now.AddHours(-1) }, Schedule);
        Assert.Throws<ArgumentException>(() => past.Activate(Schedule, Now));
    }

    [Fact]
    public void MetadataAcceptsClientCasingAndRejectsDuplicateAliases()
    {
        var config = NotificationConfig.Create(Values(Guid.NewGuid()) with
        {
            Metadata = "{\"schemaVersion\":1,\"actionType\":\"OPEN_NOTIFICATIONS\"}"
        }, Schedule);
        Assert.Contains("\"SchemaVersion\":1", config.Metadata);
        Assert.Contains("\"ActionType\":\"OPEN_NOTIFICATIONS\"", config.Metadata);
        Assert.Throws<ArgumentException>(() => NotificationConfig.Create(Values(Guid.NewGuid()) with
        {
            Metadata = "{\"SchemaVersion\":1,\"schemaVersion\":1}"
        }, Schedule));
    }

    [Fact]
    public void UserOffsetsAndEmptyCronAreNormalizedForPersistence()
    {
        var values = Values(Guid.NewGuid()) with
        {
            IsSendNow = false,
            AvailableAt = Now.AddHours(1).ToOffset(TimeSpan.FromHours(7)),
            ExpiredAfter = Now.AddHours(2).ToOffset(TimeSpan.FromHours(7)),
            CronString = "  "
        };
        var config = NotificationConfig.Create(values, Schedule);
        Assert.Equal(TimeSpan.Zero, config.AvailableAt.Offset);
        Assert.Equal(TimeSpan.Zero, config.ExpiredAfter!.Value.Offset);
        Assert.Null(config.CronString);
        config.Activate(Schedule, Now);
        Assert.Equal(Now.AddHours(1), config.NextRunAt);
    }
}
