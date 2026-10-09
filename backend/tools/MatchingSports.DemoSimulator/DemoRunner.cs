using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MatchingSports.Application;
using MatchingSports.Application.Abstractions;
using MatchingSports.Infrastructure;
using MatchingSports.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace MatchingSports.DemoSimulator;

public static class DemoRunner
{
    public static async Task<DemoReport> RunAsync(IConfiguration source, DemoOptions options, CancellationToken token = default)
    {
        options.Validate();
        var password = source["Demo:Password"];
        if (string.IsNullOrWhiteSpace(password)) throw new DemoSimulationException("Cần Demo__Password hoặc user-secrets Demo:Password.");
        var rawConnection = source.GetConnectionString("matchingsports")
            ?? throw new DemoSimulationException("Cần ConnectionStrings__matchingsports hoặc user-secrets.");
        var connection = new NpgsqlConnectionStringBuilder(rawConnection);
        if (connection.Host is not ("localhost" or "127.0.0.1" or "::1"))
            throw new DemoSimulationException("Tool seed demo chỉ chạy với PostgreSQL local.");
        if (options.TestMode) connection.Database = $"matchingsports_rbac_test_demo_{Guid.NewGuid():N}";
        if (string.IsNullOrWhiteSpace(connection.Database)) throw new DemoSimulationException("Database là bắt buộc.");
        var databaseKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{connection.Host}:{connection.Port}/{connection.Database}")))[..16];
        var directory = Path.GetFullPath(Path.Combine(options.OutputDirectory, databaseKey, options.RunId));
        Directory.CreateDirectory(directory);
        await using var fileLock = new FileStream(Path.Combine(directory, "run.lock"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
        var checkpoint = new DemoCheckpoint(directory);
        var state = await checkpoint.LoadAsync(options, databaseKey, token);
        var overrides = new Dictionary<string, string?>
        {
            ["ConnectionStrings:matchingsports"] = connection.ConnectionString,
            ["Auth:Jwt:SigningKey"] = source["Auth:Jwt:SigningKey"] ?? Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
            ["Auth:Otp:Pepper"] = source["Auth:Otp:Pepper"] ?? Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
            ["Seed:Admin:Enabled"] = "false",
            ["Teams:Notifications:EmailEnabled"] = "false",
            ["Matching:Notifications:EmailEnabled"] = "false"
        };
        var configuration = new ConfigurationBuilder().AddConfiguration(source).AddInMemoryCollection(overrides).Build();
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.ClearProviders());
        services.AddSingleton<IConfiguration>(configuration);
        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddScoped<SimulationCurrentUser>();
        services.AddScoped<ICurrentUser>(provider => provider.GetRequiredService<SimulationCurrentUser>());
        await using var provider = services.BuildServiceProvider();
        try
        {
            await provider.InitializeDatabaseAsync(true, token);
            await using var databaseLock = new NpgsqlConnection(connection.ConnectionString);
            await databaseLock.OpenAsync(token);
            await using var lockCommand = new NpgsqlCommand("SELECT pg_try_advisory_lock(741963855)", databaseLock);
            if (await lockCommand.ExecuteScalarAsync(token) is not true)
                throw new DemoSimulationException("Một DemoSimulator khác đang chạy trên database này.");
            var session = new DemoSession(provider, password, token);
            var format = await session.ReadAsync((db, ct) => db.SportFormats.AsNoTracking()
                .Where(format => format.Code == "FOOTBALL_5V5" && format.IsActive && format.Sport.IsActive)
                .Select(format => new { format.Id, format.SportId }).SingleAsync(ct));
            await new DemoSeeder(session, state, checkpoint).SeedUsersAsync(format.SportId, format.Id);
            if (state.Completed)
                return JsonSerializer.Deserialize<DemoReport>(await File.ReadAllTextAsync(checkpoint.ReportPath, token), DemoCheckpoint.Json)
                    ?? throw new DemoSimulationException("Báo cáo đã hoàn tất bị thiếu hoặc lỗi.");
            await new DemoScenario(session, state, checkpoint, format.SportId, format.Id).RunAsync();
            var report = await DemoReportBuilder.BuildAsync(session, state, options.TestMode);
            await checkpoint.SaveReportAsync(report, token);
            state.Completed = true;
            state.ActiveStep = null;
            await checkpoint.SaveAsync(state, token);
            return report;
        }
        finally
        {
            if (options.TestMode)
            {
                // Only the fresh, explicitly isolated database selected above may be deleted.
                await using var scope = provider.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>().Database.EnsureDeletedAsync(CancellationToken.None);
            }
        }
    }
}
