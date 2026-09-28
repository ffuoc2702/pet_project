using MatchingSports.Domain.Enums;

namespace MatchingSports.Domain.Entities;

public sealed class AdminAction
{
    public Guid Id { get; set; }
    public Guid AdminUserId { get; set; }
    public AdminActionType ActionType { get; set; }
    public string TargetType { get; set; } = null!;
    public Guid TargetId { get; set; }
    public string Reason { get; set; } = null!;
    public string MetadataJson { get; set; } = "{}";
    public Guid? CorrelationId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User AdminUser { get; set; } = null!;
}
