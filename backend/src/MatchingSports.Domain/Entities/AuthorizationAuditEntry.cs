namespace MatchingSports.Domain.Entities;

public sealed class AuthorizationAuditEntry
{
    private AuthorizationAuditEntry() { }
    public Guid Id { get; private set; }
    public Guid ActorUserId { get; private set; }
    public string Action { get; private set; } = null!;
    public Guid TargetId { get; private set; }
    public string BeforeJson { get; private set; } = null!;
    public string AfterJson { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    public static AuthorizationAuditEntry Create(Guid actorUserId, string action, Guid targetId,
        string beforeJson, string afterJson)
    {
        if (action is null)
            throw new ArgumentNullException(nameof(action), "Thao tác không được để trống.");
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Thao tác không được để trống.", nameof(action));
        if (actorUserId == Guid.Empty || targetId == Guid.Empty)
            throw new ArgumentException("ID người thực hiện và ID đối tượng được ghi nhật ký là bắt buộc.");
        return new AuthorizationAuditEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = action,
            TargetId = targetId,
            BeforeJson = beforeJson,
            AfterJson = afterJson,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
