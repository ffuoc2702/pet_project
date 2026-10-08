using MatchingSports.Domain.Enums;

namespace MatchingSports.Application.Features.Team;

internal static class TeamAdmission
{
    public static async Task CloseOtherApplicationsAsync(Domain.Entities.Team team, Guid userId, Guid actorId,
        DateTimeOffset now, TeamNotifications notifications, CancellationToken cancellationToken)
    {
        foreach (var invitation in team.Invitations.Where(x => x.UserId == userId && x.Status == TeamRequestStatus.PENDING))
        {
            invitation.Cancel(now);
            await notifications.PublishAsync(team, actorId, NotificationType.TEAM_INVITATION_CANCELLED,
                team.ManagementUserIds().Append(invitation.InviterUserId).Append(userId), cancellationToken, invitationId: invitation.Id, subjectUserId: invitation.UserId);
        }
        foreach (var request in team.JoinRequests.Where(x => x.UserId == userId && x.Status == TeamRequestStatus.PENDING))
        {
            request.Cancel(now);
            await notifications.PublishAsync(team, actorId, NotificationType.TEAM_JOIN_REQUEST_CANCELLED,
                team.ManagementUserIds().Append(userId), cancellationToken, joinRequestId: request.Id, subjectUserId: request.UserId);
        }
    }
}
