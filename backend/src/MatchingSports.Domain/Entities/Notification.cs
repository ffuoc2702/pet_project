using MatchingSports.Domain.Enums;
using MatchingSports.Shared;

namespace MatchingSports.Domain.Entities;

public sealed class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    /// <summary>Entity identifier from <see cref="EntityTypes"/>.</summary>
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset? ReadAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
