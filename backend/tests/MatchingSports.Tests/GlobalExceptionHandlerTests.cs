using System.Text.Json;
using MatchingSports.Api.ErrorHandling;
using MatchingSports.Application.Common;
using MatchingSports.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace MatchingSports.Tests;

public sealed class GlobalExceptionHandlerTests
{
    [Theory]
    [InlineData("application/json")]
    [InlineData("text/plain")]
    public async Task ValidationErrorsArePreservedWithSupportedAndUnsupportedAcceptHeaders(string accept)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails();
        using var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Headers.Accept = accept;
        context.Request.Path = "/api/v1/sports/test";
        context.TraceIdentifier = "validation-trace";
        using var body = new MemoryStream();
        context.Response.Body = body;
        var handler = new GlobalExceptionHandler(provider.GetRequiredService<IProblemDetailsService>(),
            NullLogger<GlobalExceptionHandler>.Instance);
        var exception = new RequestValidationException(ErrorCodes.InvalidSport, "Invalid sport.",
            new Dictionary<string, string[]> { ["formats"] = ["Format ID does not belong to this sport."] });

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);
        body.Position = 0;
        using var response = await JsonDocument.ParseAsync(body);
        var problem = response.RootElement;
        Assert.Equal(ErrorCodes.InvalidSport, problem.GetProperty("code").GetString());
        Assert.Equal("Invalid sport.", problem.GetProperty("detail").GetString());
        Assert.Equal("/api/v1/sports/test", problem.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("correlationId").GetString()));
        var errors = problem.GetProperty("errors").GetProperty("formats");
        Assert.Equal("Format ID does not belong to this sport.", Assert.Single(errors.EnumerateArray()).GetString());
    }
}
