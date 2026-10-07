using System.Text.Json;
using System.Text.Json.Nodes;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Shared;

namespace MatchingSports.Application.Features.Role;

internal static class RoleAuthorization
{
    public static void RequireCurrentVersion(string? actualStamp, string? expectedStamp)
    {
        if (string.IsNullOrWhiteSpace(expectedStamp))
            throw new RequestValidationException(ErrorCodes.ValidationFailed, "Phiên bản dữ liệu là bắt buộc.",
                new Dictionary<string, string[]> { ["concurrencyStamp"] = ["Vui lòng gửi phiên bản hiện tại của dữ liệu."] });
        if (!string.Equals(actualStamp, expectedStamp, StringComparison.Ordinal))
            throw new ConflictException(ErrorCodes.ConcurrencyConflict, "Dữ liệu đã thay đổi. Vui lòng tải lại trước khi cập nhật.");
    }

    public static void RecordChange(IAdminActionRepository repository, Guid actorUserId,
        AdminActionType actionType, string targetType, Guid targetId, object? before, object after) =>
        repository.Add(AdminAction.Create(actorUserId, actionType, targetType, targetId,
            SerializeChangeSnapshot(before), SerializeChangeSnapshot(after)));

    private static string SerializeChangeSnapshot(object? value)
    {
        var node = JsonSerializer.SerializeToNode(value);
        if (node is JsonObject details)
        {
            // Persistence stamps audit fields on commit; snapshots describe the business change.
            details.Remove("CreatedAt");
            details.Remove("UpdatedAt");
            details.Remove("CreatedBy");
            details.Remove("UpdatedBy");
        }
        return node?.ToJsonString() ?? "null";
    }

    public static T ValidateRoleInput<T>(Func<T> createValidatedValue)
    {
        try { return createValidatedValue(); }
        catch (ArgumentException exception)
        {
            throw new RequestValidationException(ErrorCodes.InvalidRole, "Thông tin vai trò không hợp lệ.",
                new Dictionary<string, string[]> { [exception.ParamName ?? "role"] = [exception.GetValidationMessage()] });
        }
    }
}
