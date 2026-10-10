using MatchingSports.Infrastructure;
using MatchingSports.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddRouting();
builder.Services.AddAutoMapper(configuration =>
{
    if (builder.Configuration["AutoMapper:LicenseKey"] is { Length: > 0 } licenseKey)
        configuration.LicenseKey = licenseKey;
}, typeof(MatchingSports.Application.DependencyInjection).Assembly);
builder.Services.AddInfrastructure(builder.Configuration);

using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILoggerFactory>()
    .CreateLogger("MatchingSports.Migrator");
var migrationStarting = LoggerMessage.Define(
    LogLevel.Information,
    new EventId(2001, "MigrationStarting"),
    "Applying database migrations and reference-data seed");
var migrationCompleted = LoggerMessage.Define(
    LogLevel.Information,
    new EventId(2002, "MigrationCompleted"),
    "Database migration completed successfully");

migrationStarting(logger, null);
await host.Services.InitializeDatabaseAsync(initialize: true);
migrationCompleted(logger, null);
