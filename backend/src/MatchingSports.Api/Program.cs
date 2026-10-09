using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Elastic.CommonSchema.Serilog;
using MatchingSports.Api.Contracts;
using MatchingSports.Api.ErrorHandling;
using MatchingSports.Api.Identity;
using MatchingSports.Api.Middleware;
using MatchingSports.Application;
using MatchingSports.Infrastructure;
using MatchingSports.Infrastructure.Persistence;
using MatchingSports.Shared;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console(new EcsTextFormatter()));

builder.AddServiceDefaults();

builder.Services.AddApplication(
    builder.Configuration["AutoMapper:LicenseKey"],
    typeof(ContractMappingProfile).Assembly);
builder.Services.AddInfrastructure(builder.Configuration);
MatchingSports.Infrastructure.Notifications.NotificationWorkerRegistration.AddNotificationWorkers(builder.Services, builder.Configuration);
MatchingSports.Infrastructure.Matching.MatchingWorkerRegistration.AddMatchingWorkers(builder.Services, builder.Configuration);
builder.Services.AddCurrentUser();
builder.Services.PostConfigure<MvcOptions>(VietnameseModelBindingMessages.LocalizeImplicitRequiredFields);
builder.Services.AddControllers(options =>
        VietnameseModelBindingMessages.Configure(options.ModelBindingMessageProvider))
    .AddJsonOptions(options =>
    {
        options.AllowInputFormatterExceptionMessages = false;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = actionContext =>
    {
        var errors = actionContext.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => JsonNamingPolicy.CamelCase.ConvertName(entry.Key),
                entry => entry.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "Giá trị không hợp lệ."
                        : error.ErrorMessage)
                    .Distinct()
                    .ToArray());

        var problemDetails = ApiProblemDetailsFactory.Create(
            actionContext.HttpContext,
            StatusCodes.Status400BadRequest,
            ErrorCodes.ValidationFailed,
            "Dữ liệu không hợp lệ",
            "Vui lòng kiểm tra lại dữ liệu đã gửi.",
            errors);

        return new BadRequestObjectResult(problemDetails)
        {
            ContentTypes = { "application/problem+json" }
        };
    };
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
        ApiProblemDetailsFactory.Enrich(context.HttpContext, context.ProblemDetails);
});
builder.Services.AddSwaggerGen();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];
var rateLimitPermitCount = builder.Configuration.GetValue<int?>("RateLimiting:PermitLimit") ?? 120;
var rateLimitWindowSeconds = builder.Configuration.GetValue<int?>("RateLimiting:WindowSeconds") ?? 60;
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
{
    if (allowedOrigins.Length > 0)
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    }
}));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("AuthStrict", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitPermitCount,
                Window = TimeSpan.FromSeconds(rateLimitWindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        var error = ApiErrorCodes.FromHttpStatus(StatusCodes.Status429TooManyRequests);
        var problemDetails = ApiProblemDetailsFactory.Create(
            context.HttpContext,
            StatusCodes.Status429TooManyRequests,
            error.Code,
            error.Title,
            error.Detail);

        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
    };
});

var app = builder.Build();

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
    app.UseHsts();
app.UseHttpsRedirection();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, _, exception) =>
        exception is not null || httpContext.Response.StatusCode >= 500
            ? LogEventLevel.Error
            : httpContext.Response.StatusCode >= 400
                ? LogEventLevel.Warning
                : LogEventLevel.Information;

    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("TraceId", ApiProblemDetailsFactory.GetTraceId(httpContext));
        diagnosticContext.Set("CorrelationId", CorrelationIdMiddleware.GetCorrelationId(httpContext));
        diagnosticContext.Set("RequestMethod", httpContext.Request.Method);
        diagnosticContext.Set("RequestPath", httpContext.Request.Path.Value ?? string.Empty);
    };
});
app.UseExceptionHandler();
app.UseStatusCodePages(async (Microsoft.AspNetCore.Diagnostics.StatusCodeContext statusCodeContext) =>
{
    var httpContext = statusCodeContext.HttpContext;
    var error = ApiErrorCodes.FromHttpStatus(httpContext.Response.StatusCode);
    var problemDetails = ApiProblemDetailsFactory.Create(
        httpContext,
        httpContext.Response.StatusCode,
        error.Code,
        error.Title,
        error.Detail);

    await Results.Json(
            problemDetails,
            statusCode: httpContext.Response.StatusCode,
            contentType: "application/problem+json")
        .ExecuteAsync(httpContext);
});
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

await app.Services.InitializeDatabaseAsync(
    app.Configuration.GetValue<bool>("Database:Initialize"),
    app.Lifetime.ApplicationStopping);

app.MapControllers();
app.MapDefaultEndpoints();
if (Directory.Exists(app.Environment.WebRootPath))
{
    app.UseFileServer();
    app.MapFallbackToFile("index.html");
}

app.Run();

public partial class Program;
