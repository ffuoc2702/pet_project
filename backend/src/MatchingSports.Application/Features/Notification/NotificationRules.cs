using System.Text.Json;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Domain.ValueObjects;
using MatchingSports.Shared;

namespace MatchingSports.Application.Features.Notification;

internal static class NotificationRules
{
    public static NotificationConfigValues Prepare(NotificationInput input, INotificationContentRenderer renderer, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.Body is null || input.Body.Length > 100000)
            throw new ArgumentException("Nội dung thông báo không hợp lệ.", nameof(input));
        var content = renderer.Render(input.Body);
        return new NotificationConfigValues(input.IsSendNow, input.ScheduleType, input.CronString, input.Critical,
            input.Title, content.Html, content.Text, input.IsSendNow ? now : input.AvailableAt, input.ExpiredAfter, input.MaxSendTimes,
            input.AudienceType, input.UserIds, input.TeamIds, input.InAppEnabled, input.EmailEnabled,
            input.PushEnabled, input.SmsEnabled, input.Metadata, input.TimeZoneId, input.Description).NormalizeAndValidate();
    }

    public static T Validate<T>(Func<T> action)
    {
        try { return action(); }
        catch (ArgumentException exception) { throw Invalid(exception); }
        catch (TimeZoneNotFoundException) { throw Invalid(new ArgumentException("Múi giờ không tồn tại.")); }
        catch (InvalidTimeZoneException) { throw Invalid(new ArgumentException("Múi giờ không hợp lệ.")); }
        catch (InvalidOperationException)
        {
            throw new ConflictException(ErrorCodes.InvalidNotificationConfig, "Cấu hình đã kết thúc hoặc đang được lập lịch.");
        }
    }

    public static void RequireVersion(NotificationConfig config, Guid token)
    {
        if (token == Guid.Empty)
            throw new RequestValidationException(ErrorCodes.InvalidNotificationConfig, "Phiên bản cấu hình là bắt buộc.",
                new Dictionary<string, string[]> { ["concurrencyToken"] = ["Vui lòng gửi phiên bản cấu hình hiện tại."] });
        if (config.ConcurrencyToken != token)
            throw new ConflictException(ErrorCodes.ConcurrencyConflict, "Cấu hình đã thay đổi. Vui lòng tải lại.");
    }

    public static void RequireChannels(NotificationConfig config, INotificationEmailSender emailSender)
        => RequireChannels(config.GetValues(), emailSender);

    public static void RequireChannels(NotificationConfigValues values, INotificationEmailSender emailSender)
    {
        if (values.PushEnabled || values.SmsEnabled || (values.EmailEnabled && !emailSender.IsConfigured))
            throw new RequestValidationException(ErrorCodes.NotificationChannelUnavailable, "Kênh gửi chưa sẵn sàng.",
                new Dictionary<string, string[]> { ["channels"] = ["Push/SMS thuộc phase sau; email yêu cầu cấu hình SMTP."] });
    }

    public static void Record(IAdminActionRepository audit, Guid actor, AdminActionType type, NotificationConfig config, object? before) =>
        audit.Add(AdminAction.Create(actor, type, EntityTypes.NotificationConfig, config.Id,
            JsonSerializer.Serialize(before), JsonSerializer.Serialize(new { Values = config.GetValues(), config.Status, config.SchedulingStatus })));

    public static object Snapshot(NotificationConfig config) => new { Values = config.GetValues(), config.Status, config.SchedulingStatus };

    private static RequestValidationException Invalid(ArgumentException exception) =>
        new(ErrorCodes.InvalidNotificationConfig, "Cấu hình thông báo không hợp lệ.",
            new Dictionary<string, string[]> { [exception.ParamName ?? "notification"] = [exception.GetValidationMessage()] });
}
