using MatchingSports.Domain.Enums;
using MatchingSports.Shared;

namespace MatchingSports.Domain.Entities;

public sealed class AdminAction
{
    private AdminAction() { }

    public Guid Id { get; private set; }
    public Guid AdminUserId { get; private set; }
    public AdminActionType ActionType { get; private set; }
    public string TargetType { get; private set; } = null!;
    public Guid TargetId { get; private set; }
    public string? Reason { get; private set; }
    public string MetadataJson { get; private set; } = "{}";
    public string BeforeJson { get; private set; } = "{}";
    public string AfterJson { get; private set; } = "{}";
    public Guid? CorrelationId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static AdminAction Create(Guid adminUserId, AdminActionType actionType, string targetType,
        Guid targetId, string beforeJson, string afterJson, string? reason = null,
        string metadataJson = "{}", Guid? correlationId = null)
    {
        if (adminUserId == Guid.Empty || targetId == Guid.Empty)
            throw new ArgumentException("ID người thực hiện và ID đối tượng được ghi nhật ký là bắt buộc.");
        if (!Enum.IsDefined(actionType))
            throw new ArgumentOutOfRangeException(nameof(actionType), "Loại thao tác không hợp lệ.");
        ArgumentException.ThrowIfNullOrWhiteSpace(targetType);
        var normalizedTargetType = targetType.Trim();
        if (normalizedTargetType.Length > 30)
            throw new ArgumentException("Loại đối tượng không được vượt quá 30 ký tự.", nameof(targetType));
        ArgumentException.ThrowIfNullOrWhiteSpace(beforeJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(afterJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(metadataJson);

        return new AdminAction
        {
            Id = Guid.NewGuid(),
            AdminUserId = adminUserId,
            ActionType = actionType,
            TargetType = normalizedTargetType,
            TargetId = targetId,
            Reason = StringNormalizer.StartEndSpaceTrim(reason),
            BeforeJson = beforeJson,
            AfterJson = afterJson,
            MetadataJson = metadataJson,
            CorrelationId = correlationId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
