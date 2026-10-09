using System.Text.Json;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Shared;

namespace MatchingSports.Application.Features.Matching;

public sealed class MatchingNotifications(IMatchingRepository matching, INotificationWorkRepository repository,
    INotificationEmailSender sender, TimeProvider clock, bool emailEnabled = false)
{
    public void RequireEmail(bool enabled)
    {
        if (enabled && !sender.IsConfigured)
            throw new ServiceUnavailableException(ErrorCodes.NotificationChannelUnavailable, "Kênh email thông báo trận chưa được cấu hình.");
    }
    public async Task<IReadOnlyList<Guid>> HostManagersAsync(Listing listing, CancellationToken cancellationToken) =>
        listing.HostTeamId is { } teamId ? await matching.TeamUsersAsync([teamId], true, cancellationToken) : [listing.OwnerUserId];
    public async Task<IReadOnlyList<Guid>> ApplicantsAsync(IEnumerable<ListingRequest> requests, CancellationToken cancellationToken) =>
        requests.Where(item => item.ApplicantUserId is not null).Select(item => item.ApplicantUserId!.Value)
            .Concat(await matching.TeamUsersAsync(requests.Where(item => item.ApplicantTeamId is not null)
                .Select(item => item.ApplicantTeamId!.Value), true, cancellationToken)).Distinct().ToArray();
    public async Task<IReadOnlyList<Guid>> ParticipantsAsync(Listing listing, CancellationToken cancellationToken)
    {
        var participants = listing.Match?.Participants.Where(item => item.Status != ParticipantStatus.CANCELLED).ToArray() ?? [];
        return participants.Where(item => item.UserId is not null).Select(item => item.UserId!.Value)
            .Concat(await matching.TeamUsersAsync(participants.Where(item => item.TeamId is not null).Select(item => item.TeamId!.Value), false, cancellationToken))
            .Concat(await HostManagersAsync(listing, cancellationToken)).Distinct().ToArray();
    }
    public async Task PublishAsync(Listing listing, Guid? actorId, NotificationType type, IEnumerable<Guid> userIds,
        CancellationToken cancellationToken, Guid? requestId = null, bool includeActor = false)
    {
        var matchEvent = type is NotificationType.MATCH_CONFIRMED or NotificationType.MATCH_CANCELLED or NotificationType.MATCH_COMPLETED or NotificationType.MATCH_REMINDER;
        var entityType = matchEvent && listing.Match is not null ? EntityTypes.Match : EntityTypes.Listing;
        var entityId = entityType == EntityTypes.Match ? listing.Match!.Id : listing.Id;
        foreach (var userId in userIds.Distinct().Where(id => includeActor || id != actorId))
            _ = await PublishOneAsync(listing, userId, actorId, type, entityType, entityId, requestId, emailEnabled, cancellationToken);
    }
    public Task<Guid?> PublishWatchAsync(Listing listing, ListingWatch watch, bool sendEmail, double distanceMeters, CancellationToken cancellationToken) =>
        PublishOneAsync(listing, watch.UserId, null, NotificationType.LISTING_WATCH_MATCHED,
            EntityTypes.Listing, listing.Id, null, sendEmail, cancellationToken, distanceMeters);

    private async Task<Guid?> PublishOneAsync(Listing listing, Guid userId, Guid? actorId, NotificationType type,
        string entityType, Guid entityId, Guid? requestId, bool sendEmail, CancellationToken cancellationToken, double? distanceMeters = null)
    {
        RequireEmail(sendEmail);
        var recipient = await repository.GetRecipientAsync(userId, cancellationToken);
        if (recipient is null) return null;
        var title = Title(type);
        var text = $"{title}: {listing.Title}. Bắt đầu {listing.StartAt:O}, tại {listing.AddressText}.";
        if (distanceMeters is { } distance) text += $" Khoảng cách {distance:0} mét.";
        var metadata = JsonSerializer.Serialize(new
        {
            SchemaVersion = 1,
            ActionType = "VIEW_ENTITY",
            EntityType = entityType,
            EntityId = entityId,
            ListingId = listing.Id,
            MatchId = listing.Match?.Id,
            RequestId = requestId,
            ActorUserId = actorId,
            StartAt = listing.StartAt,
            AddressText = listing.AddressText,
            DistanceMeters = distanceMeters
        });
        var now = clock.GetUtcNow();
        var history = NotificationHistory.CreateMatchingEvent(userId, entityType, entityId, type, title, text, metadata, now, sendEmail,
            type == NotificationType.MATCH_REMINDER ? listing.StartAt : null);
        OutboxMessage? email = null;
        if (sendEmail)
        {
            if (!recipient.EmailConfirmed || string.IsNullOrWhiteSpace(recipient.Email))
                history.RecordDelivery(NotificationChannel.EMAIL, NotificationDeliveryStatus.SKIPPED, now, "RECIPIENT_UNAVAILABLE");
            else email = new OutboxMessage
            {
                AggregateType = entityType,
                AggregateId = entityId,
                EventType = Features.Notification.NotificationJobs.EmailEvent,
                PayloadJson = JsonSerializer.Serialize(new { HistoryId = history.Id }),
                OccurredAt = now,
                AvailableAt = now
            };
        }
        repository.AddOccurrence(history, email);
        return history.Id;
    }
    private static string Title(NotificationType type) => type switch
    {
        NotificationType.REQUEST_CREATED => "Có yêu cầu tham gia kèo mới",
        NotificationType.REQUEST_ACCEPTED => "Yêu cầu tham gia đã được duyệt",
        NotificationType.REQUEST_REJECTED => "Yêu cầu tham gia đã bị từ chối",
        NotificationType.REQUEST_WITHDRAWN => "Yêu cầu tham gia đã được rút",
        NotificationType.REQUEST_EXPIRED => "Yêu cầu tham gia đã kết thúc",
        NotificationType.LISTING_UPDATED => "Thông tin kèo đã được cập nhật",
        NotificationType.LISTING_CANCELLED => "Kèo đã bị hủy",
        NotificationType.MATCH_CANCELLED => "Trận đã bị hủy",
        NotificationType.MATCH_CONFIRMED => "Trận đã được xác nhận",
        NotificationType.MATCH_REMINDER => "Sắp đến giờ bắt đầu trận đấu",
        NotificationType.MATCH_COMPLETED => "Trận đã hoàn tất",
        NotificationType.LISTING_WATCH_MATCHED => "Có kèo phù hợp để đăng ký vãng lai",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
