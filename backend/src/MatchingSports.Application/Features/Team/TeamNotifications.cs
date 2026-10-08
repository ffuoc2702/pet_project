using System.Text.Json;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Shared;

namespace MatchingSports.Application.Features.Team;

public sealed class TeamNotifications(INotificationWorkRepository repository, INotificationEmailSender sender,
    TimeProvider clock, bool emailEnabled = false)
{
    public async Task PublishAsync(Domain.Entities.Team team, Guid actorId, NotificationType type,
        IEnumerable<Guid> userIds, CancellationToken cancellationToken, Guid? invitationId = null,
        Guid? joinRequestId = null, bool includeActor = false, Guid? subjectUserId = null)
    {
        if (emailEnabled && !sender.IsConfigured)
            throw new ServiceUnavailableException(ErrorCodes.NotificationChannelUnavailable, "Kênh email thông báo đội chưa được cấu hình.");
        var title = Title(type);
        var text = $"{title}: {team.Name}.";
        var metadata = JsonSerializer.Serialize(new
        {
            SchemaVersion = 1,
            ActionType = type == NotificationType.TEAM_DELETED ? "OPEN_NOTIFICATIONS" : "VIEW_ENTITY",
            EntityType = EntityTypes.Team,
            EntityId = team.Id,
            InvitationId = invitationId,
            JoinRequestId = joinRequestId,
            ActorUserId = actorId,
            SubjectUserId = subjectUserId,
            CaptainUserId = team.CaptainUserId
        });
        foreach (var userId in userIds.Distinct().Where(id => includeActor || id != actorId))
        {
            var recipient = await repository.GetRecipientAsync(userId, cancellationToken);
            if (recipient is null) continue;
            var now = clock.GetUtcNow();
            var history = NotificationHistory.CreateTeamEvent(userId, team.Id, type, title, text, metadata, now, emailEnabled);
            OutboxMessage? email = null;
            if (emailEnabled)
            {
                if (!recipient.EmailConfirmed || string.IsNullOrWhiteSpace(recipient.Email))
                    history.RecordDelivery(NotificationChannel.EMAIL, NotificationDeliveryStatus.SKIPPED, now, "RECIPIENT_UNAVAILABLE");
                else email = new OutboxMessage
                {
                    AggregateType = EntityTypes.Team,
                    AggregateId = team.Id,
                    EventType = Features.Notification.NotificationJobs.EmailEvent,
                    PayloadJson = JsonSerializer.Serialize(new { HistoryId = history.Id }),
                    OccurredAt = now,
                    AvailableAt = now
                };
            }
            repository.AddOccurrence(history, email);
        }
    }
    private static string Title(NotificationType type) => type switch
    {
        NotificationType.TEAM_CREATED => "Đã tạo đội",
        NotificationType.TEAM_UPDATED => "Thông tin đội đã được cập nhật",
        NotificationType.TEAM_INVITATION_CREATED => "Bạn được mời vào đội",
        NotificationType.TEAM_INVITATION_CANCELLED => "Lời mời vào đội đã kết thúc",
        NotificationType.TEAM_INVITATION_ACCEPTED => "Lời mời vào đội đã được chấp nhận",
        NotificationType.TEAM_INVITATION_REJECTED => "Lời mời vào đội đã bị từ chối",
        NotificationType.TEAM_DEPUTY_APPOINTED => "Đã bổ nhiệm đội phó",
        NotificationType.TEAM_DEPUTY_REVOKED => "Đã thu hồi quyền đội phó",
        NotificationType.TEAM_JOIN_REQUEST_CREATED => "Có yêu cầu vào đội mới",
        NotificationType.TEAM_JOIN_REQUEST_WITHDRAWN => "Yêu cầu vào đội đã được rút",
        NotificationType.TEAM_JOIN_REQUEST_ACCEPTED => "Yêu cầu vào đội đã được chấp nhận",
        NotificationType.TEAM_JOIN_REQUEST_CANCELLED => "Yêu cầu vào đội đã kết thúc",
        NotificationType.TEAM_JOIN_REQUEST_REJECTED => "Yêu cầu vào đội đã bị từ chối",
        NotificationType.TEAM_MEMBER_LEFT => "Thành viên đã rời đội",
        NotificationType.TEAM_MEMBER_REMOVED => "Thành viên đã bị loại khỏi đội",
        NotificationType.TEAM_CAPTAIN_TRANSFERRED => "Đã chuyển quyền đội trưởng",
        NotificationType.TEAM_DELETED => "Đội đã bị xóa",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
