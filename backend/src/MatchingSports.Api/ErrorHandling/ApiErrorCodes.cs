namespace MatchingSports.Api.ErrorHandling;

public static class ApiErrorCodes
{
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string BadRequest = "BAD_REQUEST";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string Conflict = "CONFLICT";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    public const string InternalServerError = "INTERNAL_SERVER_ERROR";

    public static ApiErrorDescriptor FromHttpStatus(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => new(BadRequest, "Yêu cầu không hợp lệ", "Không thể xử lý yêu cầu đã gửi."),
        StatusCodes.Status401Unauthorized => new(Unauthorized, "Chưa xác thực", "Bạn cần đăng nhập để thực hiện yêu cầu này."),
        StatusCodes.Status403Forbidden => new(Forbidden, "Không có quyền truy cập", "Bạn không có quyền thực hiện yêu cầu này."),
        StatusCodes.Status404NotFound => new(ResourceNotFound, "Không tìm thấy tài nguyên", "Không tìm thấy tài nguyên được yêu cầu."),
        StatusCodes.Status409Conflict => new(Conflict, "Xung đột dữ liệu", "Yêu cầu xung đột với trạng thái hiện tại của dữ liệu."),
        StatusCodes.Status429TooManyRequests => new("RATE_LIMIT_EXCEEDED", "Quá nhiều yêu cầu", "Vui lòng thử lại sau."),
        StatusCodes.Status503ServiceUnavailable => new(ServiceUnavailable, "Dịch vụ tạm thời gián đoạn", "Vui lòng thử lại sau."),
        _ when statusCode >= 500 => new(InternalServerError, "Lỗi hệ thống", "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau."),
        _ => new($"HTTP_{statusCode}", "Không thể xử lý yêu cầu", "Yêu cầu không thể được xử lý.")
    };
}

public readonly record struct ApiErrorDescriptor(string Code, string Title, string Detail);
