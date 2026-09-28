using MatchingSports.Api.Middleware;
using MatchingSports.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace MatchingSports.Api.ErrorHandling;

public sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode = 499;
            LogClientCancelled(
                logger,
                httpContext.Request.Method,
                httpContext.Request.Path.Value ?? string.Empty);
            return true;
        }

        var error = MapException(exception);

        if (error.StatusCode >= 500)
        {
            LogUnhandledException(
                logger,
                httpContext.Request.Method,
                httpContext.Request.Path.Value ?? string.Empty,
                error.Code,
                error.StatusCode,
                exception);
        }
        else if (error.StatusCode is StatusCodes.Status409Conflict
                 or StatusCodes.Status429TooManyRequests)
        {
            LogRejectedRequestWarning(
                logger,
                httpContext.Request.Method,
                httpContext.Request.Path.Value ?? string.Empty,
                error.Code,
                error.StatusCode);
        }
        else
        {
            LogRejectedRequestInformation(
                logger,
                httpContext.Request.Method,
                httpContext.Request.Path.Value ?? string.Empty,
                error.Code,
                error.StatusCode);
        }

        httpContext.Response.StatusCode = error.StatusCode;
        httpContext.Response.Headers[CorrelationIdMiddleware.HeaderName] =
            CorrelationIdMiddleware.GetCorrelationId(httpContext);
        var problemDetails = ApiProblemDetailsFactory.Create(
            httpContext,
            error.StatusCode,
            error.Code,
            error.Title,
            error.Detail,
            error.Errors);

        var written = await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });

        if (!written && !httpContext.Response.HasStarted)
        {
            httpContext.Response.ContentType = "application/problem+json";
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            written = true;
        }

        return written;
    }

    private static MappedError MapException(Exception exception) => exception switch
    {
        RequestValidationException validation => new(
            StatusCodes.Status400BadRequest,
            validation.Code,
            "Dữ liệu không hợp lệ",
            validation.Message,
            validation.Errors.ToDictionary(entry => entry.Key, entry => entry.Value)),

        NotFoundException notFound => new(
            StatusCodes.Status404NotFound,
            notFound.Code,
            "Không tìm thấy tài nguyên",
            notFound.Message),

        ConflictException conflict => new(
            StatusCodes.Status409Conflict,
            conflict.Code,
            "Xung đột dữ liệu",
            conflict.Message),

        ServiceUnavailableException unavailable => new(
            StatusCodes.Status503ServiceUnavailable,
            unavailable.Code,
            "Dịch vụ tạm thời gián đoạn",
            unavailable.Message),

        BadHttpRequestException => new(
            StatusCodes.Status400BadRequest,
            ApiErrorCodes.BadRequest,
            "Yêu cầu không hợp lệ",
            "Không thể đọc hoặc xử lý yêu cầu đã gửi."),

        _ => new(
            StatusCodes.Status500InternalServerError,
            ApiErrorCodes.InternalServerError,
            "Lỗi hệ thống",
            "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.")
    };

    private sealed record MappedError(
        int StatusCode,
        string Code,
        string Title,
        string Detail,
        IDictionary<string, string[]>? Errors = null);

    [LoggerMessage(1001, LogLevel.Information, "Client cancelled request {RequestMethod} {RequestPath}")]
    private static partial void LogClientCancelled(
        ILogger logger,
        string requestMethod,
        string requestPath);

    [LoggerMessage(1002, LogLevel.Error, "Unhandled exception while processing {RequestMethod} {RequestPath}; {ErrorCode} {HttpStatusCode}")]
    private static partial void LogUnhandledException(
        ILogger logger,
        string requestMethod,
        string requestPath,
        string errorCode,
        int httpStatusCode,
        Exception exception);

    [LoggerMessage(1003, LogLevel.Warning, "Request rejected while processing {RequestMethod} {RequestPath}; {ErrorCode} {HttpStatusCode}")]
    private static partial void LogRejectedRequestWarning(
        ILogger logger,
        string requestMethod,
        string requestPath,
        string errorCode,
        int httpStatusCode);

    [LoggerMessage(1004, LogLevel.Information, "Request rejected while processing {RequestMethod} {RequestPath}; {ErrorCode} {HttpStatusCode}")]
    private static partial void LogRejectedRequestInformation(
        ILogger logger,
        string requestMethod,
        string requestPath,
        string errorCode,
        int httpStatusCode);
}
