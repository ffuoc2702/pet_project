using MatchingSports.Domain.Enums;

namespace MatchingSports.Domain.Entities;

public sealed class TeamInvitation : ConcurrentAuditableEntity
{
    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid InviterUserId { get; private set; }
    public TeamRequestStatus Status { get; private set; } = TeamRequestStatus.PENDING;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RespondedAt { get; private set; }
    public Team Team { get; private set; } = null!;
    public User User { get; private set; } = null!;

    public static TeamInvitation Create(Guid teamId, Guid userId, Guid inviterId, DateTimeOffset now, SystemSettings settings)
    {
        TeamRules.Require(teamId != Guid.Empty && userId != Guid.Empty && inviterId != Guid.Empty && userId != inviterId,
            "Đội, người mời và người nhận không hợp lệ.");
        return new TeamInvitation
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            UserId = userId,
            InviterUserId = inviterId,
            ExpiresAt = settings.GetTeamInvitationExpiresAt(now)
        };
    }

    public void Accept(DateTimeOffset now) => Respond(TeamRequestStatus.ACCEPTED, now);
    public void Reject(DateTimeOffset now) => Respond(TeamRequestStatus.REJECTED, now);
    public void Cancel(DateTimeOffset now)
    {
        TeamRules.Require(Status == TeamRequestStatus.PENDING, "Lời mời đã được xử lý.");
        Status = now >= ExpiresAt ? TeamRequestStatus.EXPIRED : TeamRequestStatus.CANCELLED;
        RespondedAt = now;
    }
    public void Expire(DateTimeOffset now)
    {
        if (Status == TeamRequestStatus.PENDING && now >= ExpiresAt)
        {
            Status = TeamRequestStatus.EXPIRED;
            RespondedAt = now;
        }
    }
    public void RequirePending(DateTimeOffset now) =>
        TeamRules.Require(Status == TeamRequestStatus.PENDING && now < ExpiresAt, "Lời mời đã được xử lý hoặc hết hạn.");
    private void Respond(TeamRequestStatus status, DateTimeOffset now)
    {
        RequirePending(now);
        Status = status;
        RespondedAt = now;
    }
}
