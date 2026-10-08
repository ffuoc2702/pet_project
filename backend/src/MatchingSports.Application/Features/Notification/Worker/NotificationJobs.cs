using System.Text.Json;
using MatchingSports.Application.Abstractions;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Domain.Services;
using MatchingSports.Shared;

namespace MatchingSports.Application.Features.Notification;

public sealed class NotificationDeliveryException(string code) : Exception("Không thể gửi thông báo.")
{
    public string Code { get; } = code;
}

public sealed class NotificationJobs(INotificationWorkRepository repository, INotificationSchedule schedule,
    INotificationEmailSender emailSender, IUnitOfWork unitOfWork, IAuditActorContext actorContext, TimeProvider clock)
{
    public const string EmailEvent = "NotificationEmailRequested";

    public async Task<bool> GenerateNextOccurrenceAsync(CancellationToken cancellationToken)
    {
        using var actorScope = actorContext.BeginScope(null);
        Guid configId;
        Guid token;
        await using (var claim = await repository.BeginAsync(cancellationToken))
        {
            var config = await repository.LockDueConfigAsync(clock.GetUtcNow(), cancellationToken);
            if (config is null) return false;
            configId = config.Id;
            token = config.Claim(clock.GetUtcNow(), TimeSpan.FromMinutes(5));
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await claim.CommitAsync(cancellationToken);
        }

        await using var transaction = await repository.BeginAsync(cancellationToken, repeatableRead: true);
        var claimedConfig = await repository.LockConfigAsync(configId, cancellationToken);
        if (claimedConfig is null || !claimedConfig.OwnsLease(token, clock.GetUtcNow())) return true;
        if (!ValidateClaimedSchedule(claimedConfig))
            claimedConfig.Abort("INVALID_SCHEDULE");
        else if (claimedConfig.HasExpired(clock.GetUtcNow()))
            claimedConfig.CompleteExpired();
        else
        {
            var recipients = await repository.GetRecipientsAsync(claimedConfig.GetValues(), cancellationToken);
            foreach (var recipient in recipients)
            {
                var history = NotificationHistory.Create(claimedConfig, recipient.UserId, recipient.SourceTeamIds);
                OutboxMessage? email = null;
                if (claimedConfig.EmailEnabled)
                {
                    if (!recipient.EmailConfirmed || string.IsNullOrWhiteSpace(recipient.Email))
                        history.RecordDelivery(NotificationChannel.EMAIL, NotificationDeliveryStatus.SKIPPED, clock.GetUtcNow(), "RECIPIENT_UNAVAILABLE");
                    else
                        email = new OutboxMessage
                        {
                            AggregateType = EntityTypes.NotificationConfig,
                            AggregateId = claimedConfig.Id,
                            EventType = EmailEvent,
                            PayloadJson = JsonSerializer.Serialize(new { HistoryId = history.Id }),
                            OccurredAt = clock.GetUtcNow(),
                            AvailableAt = clock.GetUtcNow()
                        };
                }
                repository.AddOccurrence(history, email);
            }
            claimedConfig.CompleteOccurrence(schedule, clock.GetUtcNow());
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DispatchNextEmailAsync(CancellationToken cancellationToken)
    {
        using var actorScope = actorContext.BeginScope(null);
        // Hold row locks through delivery: cancellation is serialized with the send boundary.
        await using var transaction = await repository.BeginAsync(cancellationToken);
        var message = await repository.LockDueMessageAsync(clock.GetUtcNow(), cancellationToken);
        if (message is null) return false;
        Guid historyId;
        try
        {
            using var payload = JsonDocument.Parse(message.PayloadJson);
            historyId = payload.RootElement.GetProperty("HistoryId").GetGuid();
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            message.FailPermanently("INVALID_NOTIFICATION_PAYLOAD", clock.GetUtcNow());
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        var config = message.AggregateType == EntityTypes.NotificationConfig
            ? await repository.LockConfigAsync(message.AggregateId, cancellationToken) : null;
        var history = await repository.GetHistoryForDeliveryAsync(historyId, cancellationToken);
        if (history is not null && (history.NotificationConfigId is { } configId
            ? message.AggregateType != EntityTypes.NotificationConfig || configId != message.AggregateId
            : message.AggregateType != EntityTypes.Team || history.TeamId != message.AggregateId))
            message.FailPermanently("NOTIFICATION_CONFIG_MISMATCH", clock.GetUtcNow());
        else if (history is null)
            message.MarkProcessed(clock.GetUtcNow());
        else if (history.EmailStatus != NotificationDeliveryStatus.PENDING)
            message.MarkProcessed(clock.GetUtcNow());
        else if ((history.NotificationConfigId is not null && (config is null || config.Status == NotificationConfigStatus.CANCEL))
            || history.ExpiresAt <= clock.GetUtcNow())
        {
            history.RecordDelivery(NotificationChannel.EMAIL, NotificationDeliveryStatus.SKIPPED, clock.GetUtcNow(), "CANCELLED_OR_EXPIRED");
            message.MarkProcessed(clock.GetUtcNow());
        }
        else
        {
            var recipient = history.UserId is { } userId ? await repository.GetRecipientAsync(userId, cancellationToken) : null;
            if (recipient is null || !recipient.EmailConfirmed || string.IsNullOrWhiteSpace(recipient.Email))
            {
                history.RecordDelivery(NotificationChannel.EMAIL, NotificationDeliveryStatus.SKIPPED, clock.GetUtcNow(), "RECIPIENT_UNAVAILABLE");
                message.MarkProcessed(clock.GetUtcNow());
            }
            else
            {
                try
                {
                    await emailSender.SendAsync(recipient.Email, history.Title, history.Body,
                        $"notification:{history.Id}:email", cancellationToken);
                    history.RecordDelivery(NotificationChannel.EMAIL, NotificationDeliveryStatus.SENT, clock.GetUtcNow());
                    message.MarkProcessed(clock.GetUtcNow());
                }
                catch (NotificationDeliveryException exception)
                {
                    message.RecordDeliveryFailure(exception.Code, await repository.GetSettingsAsync(cancellationToken), clock.GetUtcNow());
                    if (message.FailedAt is not null)
                        history.RecordDelivery(NotificationChannel.EMAIL, NotificationDeliveryStatus.FAILED, clock.GetUtcNow(), exception.Code);
                }
            }
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public Task<int> PurgeExpiredAsync(int batchSize, CancellationToken cancellationToken) =>
        repository.PurgeBatchAsync(clock.GetUtcNow(), batchSize, cancellationToken);

    private bool ValidateClaimedSchedule(NotificationConfig config)
    {
        try
        {
            _ = schedule.GetNextOccurrence(config.CronString ?? "* * * * *", config.TimeZoneId, clock.GetUtcNow());
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or TimeZoneNotFoundException or InvalidTimeZoneException)
        { return false; }
    }
}
