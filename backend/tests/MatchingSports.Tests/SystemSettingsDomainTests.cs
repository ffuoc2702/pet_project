using MatchingSports.Domain.Entities;
using MatchingSports.Domain.ValueObjects;

namespace MatchingSports.Tests;

public sealed class SystemSettingsDomainTests
{
    [Theory]
    [InlineData("retry", -1)]
    [InlineData("retry", 101)]
    [InlineData("captain", -1)]
    [InlineData("member", -1)]
    [InlineData("deputy", -1)]
    [InlineData("deputy", 50)]
    [InlineData("capacity", 0)]
    [InlineData("invitation", 0)]
    [InlineData("pending", -1)]
    [InlineData("initial", 0)]
    [InlineData("maximum", 4)]
    [InlineData("maximum", 604801)]
    [InlineData("outboxRetention", 0)]
    [InlineData("notificationRetention", 0)]
    public void InvalidConfigurationDoesNotPartiallyChangeSettings(string field, int value)
    {
        var settings = SystemSettings.CreateDefaults();
        var before = settings.GetValues();
        var invalid = field switch
        {
            "retry" => before with { OutboxMaxRetryCount = value },
            "captain" => before with { MaxCaptainTeamsPerUser = value },
            "member" => before with { MaxMemberTeamsPerUser = value },
            "deputy" => before with { MaxDeputiesPerTeam = value },
            "capacity" => before with { MaxMembersPerTeam = value },
            "invitation" => before with { TeamInvitationLifetimeHours = value },
            "pending" => before with { MaxPendingTeamRequests = value },
            "initial" => before with { OutboxRetryInitialDelaySeconds = value },
            "maximum" => before with { OutboxRetryMaxDelaySeconds = value },
            "outboxRetention" => before with { ProcessedOutboxRetentionDays = value },
            _ => before with { NotificationRetentionDays = value }
        };
        Assert.Throws<ArgumentOutOfRangeException>(() => settings.UpdateValues(invalid));
        Assert.Equal(before, settings.GetValues());
    }

    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 10)]
    [InlineData(3, 20)]
    [InlineData(4, 40)]
    [InlineData(5, 60)]
    [InlineData(100, 60)]
    public void BackoffDoublesInitialDelayAndClampsWithoutOverflow(int retryNumber, int seconds)
    {
        var settings = SystemSettings.CreateDefaults();
        settings.UpdateValues(settings.GetValues() with { OutboxMaxRetryCount = 100, OutboxRetryMaxDelaySeconds = 60 });
        Assert.Equal(TimeSpan.FromSeconds(seconds), settings.GetOutboxRetryDelay(retryNumber));
    }

    [Fact]
    public void RetryLimitCountsRetriesAfterTheInitialAttemptAndStopsPendingProcessing()
    {
        var settings = SystemSettings.CreateDefaults();
        settings.UpdateValues(settings.GetValues() with { OutboxMaxRetryCount = 2 });
        var now = new DateTimeOffset(2026, 10, 7, 0, 0, 0, TimeSpan.Zero);
        var message = new OutboxMessage { AvailableAt = now };
        Assert.True(message.CanProcess(now));

        message.RecordDeliveryFailure("Delivery failed", settings, now);
        Assert.Equal(1, message.RetryCount);
        Assert.Equal(now.AddSeconds(5), message.AvailableAt);
        Assert.False(message.CanProcess(now));
        Assert.True(message.CanProcess(message.AvailableAt));

        now = message.AvailableAt;
        message.RecordDeliveryFailure("Still unavailable", settings, now);
        Assert.Equal(2, message.RetryCount);
        Assert.Equal(now.AddSeconds(10), message.AvailableAt);
        now = message.AvailableAt;
        message.RecordDeliveryFailure("Final failure", settings, now);
        Assert.Equal(2, message.RetryCount);
        Assert.Equal(now, message.FailedAt);
        Assert.Null(message.ProcessedAt);
        Assert.False(message.CanProcess(now.AddDays(1)));
        Assert.Throws<InvalidOperationException>(() => message.MarkProcessed(now));
    }

    [Fact]
    public void ZeroRetriesStopsImmediatelyAndSuccessClearsErrorAndLock()
    {
        var settings = SystemSettings.CreateDefaults();
        settings.UpdateValues(settings.GetValues() with { OutboxMaxRetryCount = 0 });
        var now = DateTimeOffset.UtcNow;
        var failed = new OutboxMessage();
        failed.RecordDeliveryFailure("Failed", settings, now);
        Assert.Equal(0, failed.RetryCount);
        Assert.Equal(now, failed.FailedAt);

        var success = new OutboxMessage { LastError = "Previous failure", LockedUntil = now.AddMinutes(1) };
        success.MarkProcessed(now);
        Assert.Equal(now, success.ProcessedAt);
        Assert.Null(success.LastError);
        Assert.Null(success.LockedUntil);
        Assert.False(success.CanProcess(now.AddDays(1)));
    }

    [Fact]
    public void RetentionAndInvitationDeadlinesUseCreationTimeAndExistingDeadlinesStayUnchanged()
    {
        var settings = SystemSettings.CreateDefaults();
        var createdAt = new DateTimeOffset(2026, 10, 7, 12, 30, 0, TimeSpan.Zero);
        var notification = new Notification();
        var message = new OutboxMessage();
        // Rehydrate creation metadata as persistence does; public setters are intentionally unavailable.
        typeof(AuditableEntity).GetProperty(nameof(AuditableEntity.CreatedAt))!.SetValue(notification, createdAt);
        typeof(AuditableEntity).GetProperty(nameof(AuditableEntity.CreatedAt))!.SetValue(message, createdAt);
        notification.InitializeRetention(settings);
        message.InitializeRetention(settings);
        Assert.Equal(createdAt.AddDays(90), notification.PurgeAfter);
        Assert.Equal(createdAt.AddDays(30), message.PurgeAfter);
        Assert.Equal(createdAt.AddHours(72), settings.GetTeamInvitationExpiresAt(createdAt));

        settings.UpdateValues(settings.GetValues() with { NotificationRetentionDays = 1, ProcessedOutboxRetentionDays = 2 });
        message.MarkProcessed(createdAt.AddDays(20));
        Assert.Equal(createdAt.AddDays(30), message.PurgeAfter);
        Assert.Equal(createdAt.AddDays(90), notification.PurgeAfter);
    }

    [Fact]
    public void TeamLimitsAllowAvailableCapacityAndRejectAtOrAboveTheLimit()
    {
        var settings = SystemSettings.CreateDefaults();
        settings.EnsureCanCaptainAnotherTeam(2);
        settings.EnsureCanJoinAnotherTeam(9, 49);
        settings.EnsureCanDelegateAnotherDeputy(1);
        settings.EnsureCanCreateTeamRequest(99);
        Assert.Throws<ArgumentException>(() => settings.EnsureCanCaptainAnotherTeam(3));
        Assert.Throws<ArgumentException>(() => settings.EnsureCanCaptainAnotherTeam(4));
        Assert.Throws<ArgumentException>(() => settings.EnsureCanJoinAnotherTeam(10, 1));
        Assert.Throws<ArgumentException>(() => settings.EnsureCanJoinAnotherTeam(1, 50));
        Assert.Throws<ArgumentException>(() => settings.EnsureCanDelegateAnotherDeputy(2));
        Assert.Throws<ArgumentException>(() => settings.EnsureCanCreateTeamRequest(100));
    }

    [Fact]
    public void ZeroOptionalLimitsDisableNewActionsAndOneMemberTeamsCannotHaveDeputies()
    {
        var settings = SystemSettings.CreateDefaults();
        settings.UpdateValues(SystemSettingsValues.Defaults with
        {
            MaxCaptainTeamsPerUser = 0,
            MaxMemberTeamsPerUser = 0,
            MaxDeputiesPerTeam = 0,
            MaxMembersPerTeam = 1,
            MaxPendingTeamRequests = 0
        });
        Assert.Throws<ArgumentException>(() => settings.EnsureCanCaptainAnotherTeam(0));
        Assert.Throws<ArgumentException>(() => settings.EnsureCanJoinAnotherTeam(0, 0));
        Assert.Throws<ArgumentException>(() => settings.EnsureCanDelegateAnotherDeputy(0));
        Assert.Throws<ArgumentException>(() => settings.EnsureCanCreateTeamRequest(0));
    }
}
