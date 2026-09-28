using System.Diagnostics;

namespace MatchingSports.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";
    private const string ItemName = "MatchingSports.CorrelationId";
    private const int MaxCorrelationIdLength = 128;

    public async Task InvokeAsync(HttpContext context)
    {
        var supplied = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = IsValid(supplied)
            ? supplied!
            : Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        context.Items[ItemName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (context.RequestServices.GetRequiredService<ILogger<CorrelationIdMiddleware>>()
                    .BeginScope(new Dictionary<string, object?>
                    {
                        ["TraceId"] = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier,
                        ["TransactionId"] = Activity.Current?.SpanId.ToString(),
                        ["CorrelationId"] = correlationId
                    }))
        {
            await next(context);
        }
    }

    public static string GetCorrelationId(HttpContext context) =>
        context.Items.TryGetValue(ItemName, out var value) && value is string correlationId
            ? correlationId
            : context.TraceIdentifier;

    private static bool IsValid(string? correlationId) =>
        !string.IsNullOrWhiteSpace(correlationId)
        && correlationId.Length <= MaxCorrelationIdLength
        && correlationId.All(character =>
            char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.' or ':');
}
