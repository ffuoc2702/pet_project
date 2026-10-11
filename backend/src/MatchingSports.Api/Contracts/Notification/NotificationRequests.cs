using System.ComponentModel.DataAnnotations;
using MatchingSports.Shared;

namespace MatchingSports.Api.Contracts;

public class NotificationConfigRequest
{
    [StringLength(ResourceLimits.DescriptionMaxLength, ErrorMessage = "Mô tả không được vượt quá 200 ký tự.")]
    public string? Description { get; init; }
    public bool IsSendNow { get; init; }
    [Required(ErrorMessage = "Loại lịch là bắt buộc.")]
    [RegularExpression("^(ONCE|SCHEDULED)$", ErrorMessage = "Loại lịch không hợp lệ.")]
    public string ScheduleType { get; init; } = "ONCE";
    [StringLength(120, ErrorMessage = "Cron tối đa 120 ký tự.")]
    public string? CronString { get; init; }
    [Required(ErrorMessage = "Mức độ thông báo là bắt buộc.")]
    [RegularExpression("^(INFO|WARN|CRIT)$", ErrorMessage = "Mức độ thông báo không hợp lệ.")]
    public string Critical { get; init; } = "INFO";
    [Required(ErrorMessage = "Tiêu đề là bắt buộc.")]
    [StringLength(160, ErrorMessage = "Tiêu đề tối đa 160 ký tự.")]
    public string Title { get; init; } = null!;
    [Required(ErrorMessage = "Nội dung là bắt buộc.")]
    [StringLength(100000, ErrorMessage = "Nội dung tối đa 100000 ký tự.")]
    public string Body { get; init; } = null!;
    public DateTimeOffset AvailableAt { get; init; }
    public DateTimeOffset? ExpiredAfter { get; init; }
    [Range(1, int.MaxValue, ErrorMessage = "Giới hạn lượt gửi phải lớn hơn 0.")]
    public int? MaxSendTimes { get; init; }
    [Required(ErrorMessage = "Loại đối tượng nhận là bắt buộc.")]
    [RegularExpression("^(SELECTED_USERS|SELECTED_TEAMS|ALL_USERS)$", ErrorMessage = "Loại đối tượng nhận không hợp lệ.")]
    public string AudienceType { get; init; } = "SELECTED_USERS";
    [MaxLength(10000, ErrorMessage = "Danh sách user tối đa 10000 mục.")]
    public IReadOnlyCollection<Guid> UserIds { get; init; } = [];
    [MaxLength(10000, ErrorMessage = "Danh sách team tối đa 10000 mục.")]
    public IReadOnlyCollection<Guid> TeamIds { get; init; } = [];
    public bool InAppEnabled { get; init; }
    public bool EmailEnabled { get; init; }
    public bool PushEnabled { get; init; }
    public bool SmsEnabled { get; init; }
    public System.Text.Json.JsonElement Metadata { get; init; } =
        System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>("{\"SchemaVersion\":1}");
    [Required(ErrorMessage = "Múi giờ là bắt buộc.")]
    [StringLength(100, ErrorMessage = "Múi giờ tối đa 100 ký tự.")]
    public string TimeZoneId { get; init; } = "Asia/Ho_Chi_Minh";
}
public sealed class UpdateNotificationConfigRequest : NotificationConfigRequest
{
    public Guid ConcurrencyToken { get; init; }
}
public sealed class NotificationVersionRequest
{
    public Guid ConcurrencyToken { get; init; }
}
public sealed class NotificationPreviewPaginationRequest : PaginatedRequest;
public sealed class GetNotificationConfigsRequest : BaseRequest
{
    [RegularExpression("^(DRAFT|ACTIVE|CANCEL)$", ErrorMessage = "Trạng thái cấu hình không hợp lệ.")]
    public string? Status { get; init; }
}
public sealed class GetNotificationHistoryRequest : BaseRequest
{
    public Guid? NotificationConfigId { get; init; }
    public bool UnreadOnly { get; init; }
}
