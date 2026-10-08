using MatchingSports.Application.Features.Notification;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MatchingSports.Infrastructure.Notifications;

internal sealed partial class NotificationWorker(IServiceScopeFactory scopes, TimeProvider clock, ILogger<NotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        DateOnly? lastCleanupDate = null;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                for (var index = 0; index < 100; index++)
                {
                    await using var scope = scopes.CreateAsyncScope();
                    var jobs = scope.ServiceProvider.GetRequiredService<NotificationJobs>();
                    var scheduled = await jobs.GenerateNextOccurrenceAsync(stoppingToken);
                    var delivered = await jobs.DispatchNextEmailAsync(stoppingToken);
                    if (!scheduled && !delivered) break;
                }
                var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
                if (today != lastCleanupDate)
                {
                    for (var index = 0; index < 100; index++)
                    {
                        await using var scope = scopes.CreateAsyncScope();
                        var count = await scope.ServiceProvider.GetRequiredService<NotificationJobs>().PurgeExpiredAsync(500, stoppingToken);
                        if (count == 0) { lastCleanupDate = today; break; }
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                // No message content, addresses, credentials or provider errors in worker logs.
                LogCycleInterrupted(logger, exception.GetType().Name);
            }
            try { await Task.Delay(TimeSpan.FromSeconds(5), clock, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }

    [LoggerMessage(2201, LogLevel.Error, "Chu kỳ thông báo bị gián đoạn ({ErrorType}); worker sẽ thử lại.")]
    private static partial void LogCycleInterrupted(ILogger logger, string errorType);
}

public static class NotificationWorkerRegistration
{
    public static IServiceCollection AddNotificationWorkers(this IServiceCollection services, IConfiguration configuration)
    {
        if (configuration.GetValue("Notifications:WorkersEnabled", true))
            services.AddHostedService<NotificationWorker>();
        return services;
    }
}
