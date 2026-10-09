using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Notification;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.ValueObjects;

namespace MatchingSports.Application.Abstractions;

public interface INotificationRepository
{
    void Add(NotificationConfig config);
    Task<NotificationConfig?> GetConfigAsync(Guid id, bool tracked, CancellationToken cancellationToken);
    Task<PaginatedResult<NotificationConfig>> ListConfigsAsync(GetNotificationConfigsQuery query, CancellationToken cancellationToken);
    Task ValidateTargetsAsync(NotificationConfigValues values, CancellationToken cancellationToken);
    Task<PaginatedResult<NotificationRecipient>> PreviewRecipientsAsync(NotificationConfigValues values, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<PaginatedResult<NotificationHistory>> ListHistoryAsync(NotificationHistoryQuery query, CancellationToken cancellationToken);
    Task<NotificationHistory?> GetOwnHistoryAsync(Guid id, Guid userId, bool tracked, DateTimeOffset now, CancellationToken cancellationToken);
    Task<long> CountUnreadAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);
}

public sealed record NotificationRecipient(Guid UserId, string? Email, bool EmailConfirmed, IReadOnlyCollection<Guid> SourceTeamIds);

public interface INotificationContentRenderer
{
    (string Html, string Text) Render(string body);
}

public interface INotificationEmailSender
{
    bool IsConfigured { get; }
    Task SendAsync(string email, string title, string html, string idempotencyKey, CancellationToken cancellationToken);
}

public interface INotificationTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}

public interface INotificationWorkRepository
{
    Task<INotificationTransaction> BeginAsync(CancellationToken cancellationToken, bool repeatableRead = false);
    Task<NotificationConfig?> LockDueConfigAsync(DateTimeOffset now, CancellationToken cancellationToken);
    Task<NotificationConfig?> LockConfigAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<NotificationRecipient>> GetRecipientsAsync(NotificationConfigValues values, CancellationToken cancellationToken);
    void AddOccurrence(NotificationHistory history, OutboxMessage? email);
    Task<OutboxMessage?> LockDueMessageAsync(DateTimeOffset now, CancellationToken cancellationToken);
    Task<NotificationHistory?> GetHistoryForDeliveryAsync(Guid id, CancellationToken cancellationToken);
    Task<Match?> LockReminderMatchAsync(Guid matchId, CancellationToken cancellationToken);
    Task<SystemSettings> GetSettingsAsync(CancellationToken cancellationToken);
    Task<NotificationRecipient?> GetRecipientAsync(Guid userId, CancellationToken cancellationToken);
    Task<int> PurgeBatchAsync(DateTimeOffset now, int batchSize, CancellationToken cancellationToken);
}
