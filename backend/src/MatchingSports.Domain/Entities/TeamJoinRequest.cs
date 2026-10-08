using MatchingSports.Domain.Enums;
using MatchingSports.Shared;

namespace MatchingSports.Domain.Entities;

public sealed class TeamJoinRequest : ConcurrentAuditableEntity
{
    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid UserId { get; private set; }
    public string? Message { get; private set; }
    public TeamRequestStatus Status { get; private set; } = TeamRequestStatus.PENDING;
    public Guid? ReviewedBy { get; private set; }
    public DateTimeOffset? RespondedAt { get; private set; }
    public Team Team { get; private set; } = null!;
    public User User { get; private set; } = null!;

    public static TeamJoinRequest Create(Guid teamId, Guid userId, string? message)
    {
        TeamRules.Require(teamId != Guid.Empty && userId != Guid.Empty && (message is null || message.Length <= 1000),
            "Yêu cầu vào đội không hợp lệ.");
        return new TeamJoinRequest
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            UserId = userId,
            Message = StringNormalizer.StartEndSpaceTrim(message)
        };
    }
    public void RequirePending() => TeamRules.Require(Status == TeamRequestStatus.PENDING, "Yêu cầu đã được xử lý.");
    public void Accept(Guid reviewer, DateTimeOffset now) => Respond(TeamRequestStatus.ACCEPTED, reviewer, now);
    public void Reject(Guid reviewer, DateTimeOffset now) => Respond(TeamRequestStatus.REJECTED, reviewer, now);
    public void Withdraw(DateTimeOffset now) => Respond(TeamRequestStatus.WITHDRAWN, null, now);
    public void Cancel(DateTimeOffset now) => Respond(TeamRequestStatus.CANCELLED, null, now);
    private void Respond(TeamRequestStatus status, Guid? reviewer, DateTimeOffset now)
    {
        RequirePending();
        TeamRules.Require(reviewer != Guid.Empty, "Người duyệt không hợp lệ.");
        Status = status; ReviewedBy = reviewer; RespondedAt = now;
    }
}
