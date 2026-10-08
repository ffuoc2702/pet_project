using MatchingSports.Shared;

namespace MatchingSports.Api.ErrorHandling;

public static class ApiErrorCodes
{
    public static ApiErrorDescriptor FromHttpStatus(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => new(ErrorCodes.BadRequest, "Yêu cầu không hợp lệ", "Không thể xử lý yêu cầu đã gửi."),
        StatusCodes.Status401Unauthorized => new(ErrorCodes.Unauthorized, "Chưa xác thực", "Bạn cần đăng nhập để thực hiện yêu cầu này."),
        StatusCodes.Status403Forbidden => new(ErrorCodes.Forbidden, "Không có quyền truy cập", "Bạn không có quyền thực hiện yêu cầu này."),
        StatusCodes.Status404NotFound => new(ErrorCodes.ResourceNotFound, "Không tìm thấy tài nguyên", "Không tìm thấy tài nguyên được yêu cầu."),
        StatusCodes.Status409Conflict => new(ErrorCodes.Conflict, "Xung đột dữ liệu", "Yêu cầu xung đột với trạng thái hiện tại của dữ liệu."),
        StatusCodes.Status429TooManyRequests => new(ErrorCodes.RateLimitExceeded, "Quá nhiều yêu cầu", "Vui lòng thử lại sau."),
        StatusCodes.Status503ServiceUnavailable => new(ErrorCodes.ServiceUnavailable, "Dịch vụ tạm thời gián đoạn", "Vui lòng thử lại sau."),
        _ when statusCode >= 500 => new(ErrorCodes.InternalServerError, "Lỗi hệ thống", "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau."),
        _ => new(ErrorCodes.FromHttpStatus(statusCode), "Không thể xử lý yêu cầu", "Yêu cầu không thể được xử lý.")
    };
}

public readonly record struct ApiErrorDescriptor(string Code, string Title, string Detail);
