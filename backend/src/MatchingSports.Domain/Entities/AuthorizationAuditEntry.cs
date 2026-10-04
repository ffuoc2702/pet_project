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
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        if (actorUserId == Guid.Empty || targetId == Guid.Empty)
            throw new ArgumentException("Audit actor and target identifiers are required.");
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
