using System.Diagnostics;
using MatchingSports.Api.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace MatchingSports.Api.ErrorHandling;

public static class ApiProblemDetailsFactory
{
    public static ProblemDetails Create(
        HttpContext httpContext,
        int statusCode,
        string code,
        string title,
        string detail,
        IDictionary<string, string[]>? errors = null)
    {
        ProblemDetails problemDetails = errors is null
            ? new ProblemDetails()
            : new ValidationProblemDetails(errors);

        problemDetails.Status = statusCode;
        problemDetails.Type = $"/problems/{code.ToLowerInvariant().Replace('_', '-')}";
        problemDetails.Title = title;
        problemDetails.Detail = detail;
        problemDetails.Instance = httpContext.Request.Path;
        problemDetails.Extensions["code"] = code;

        Enrich(httpContext, problemDetails);
        return problemDetails;
    }

    public static void Enrich(HttpContext httpContext, ProblemDetails problemDetails)
    {
        problemDetails.Status ??= httpContext.Response.StatusCode;
        problemDetails.Instance ??= httpContext.Request.Path;
        problemDetails.Extensions["code"] ??=
            ApiErrorCodes.FromHttpStatus(problemDetails.Status.Value).Code;
        problemDetails.Extensions["traceId"] = GetTraceId(httpContext);
        problemDetails.Extensions["correlationId"] =
            CorrelationIdMiddleware.GetCorrelationId(httpContext);
    }

    public static string GetTraceId(HttpContext httpContext) =>
        Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;
}
