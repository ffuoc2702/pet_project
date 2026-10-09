using MatchingSports.Application.Features.Matching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MatchingSports.Infrastructure.Matching;

internal sealed partial class MatchingWorker(IServiceScopeFactory scopes, TimeProvider clock, ILogger<MatchingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                for (var index = 0; index < 100; index++)
                {
                    await using var scope = scopes.CreateAsyncScope();
                    if (!await scope.ServiceProvider.GetRequiredService<MatchingJobs>().ProcessNextAsync(stoppingToken)) break;
                }
                for (var index = 0; index < 10; index++)
                {
                    await using var scope = scopes.CreateAsyncScope();
                    if (await scope.ServiceProvider.GetRequiredService<MatchingJobs>().PublishRemindersBatchAsync(stoppingToken) == 0) break;
                }
                for (var index = 0; index < 10; index++)
                {
                    await using var scope = scopes.CreateAsyncScope();
                    if (await scope.ServiceProvider.GetRequiredService<MatchingJobs>().ExpireBatchAsync(stoppingToken) == 0) break;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { LogCycleInterrupted(logger, exception.GetType().Name); }
            try { await Task.Delay(TimeSpan.FromSeconds(5), clock, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
    [LoggerMessage(2301, LogLevel.Error, "Chu kỳ tìm trận bị gián đoạn ({ErrorType}); worker sẽ thử lại.")]
    private static partial void LogCycleInterrupted(ILogger logger, string errorType);
}

public static class MatchingWorkerRegistration
{
    public static IServiceCollection AddMatchingWorkers(this IServiceCollection services, IConfiguration configuration)
    {
        if (configuration.GetValue("Matching:WorkersEnabled", true)) services.AddHostedService<MatchingWorker>();
        return services;
    }
}
