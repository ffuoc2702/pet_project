using System.Text.Json;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Entities;
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

    public static void RecordChange(IAuthorizationAuditRepository repository, Guid actorUserId,
        string action, Guid targetId, object? before, object after) =>
        repository.Add(AuthorizationAuditEntry.Create(actorUserId, action, targetId,
            JsonSerializer.Serialize(before), JsonSerializer.Serialize(after)));

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
