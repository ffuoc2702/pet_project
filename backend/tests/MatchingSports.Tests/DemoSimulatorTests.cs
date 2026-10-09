using System.Text.Json;
using MatchingSports.DemoSimulator;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace MatchingSports.Tests;

public sealed class DemoSimulatorTests
{
    [Theory]
    [InlineData("--run-id", "../escape")]
    [InlineData("--mode", "production")]
    [InlineData("--users", "15")]
    [InlineData("--teams", "20")]
    [InlineData("--listings", "7")]
    [InlineData("--unknown", "1")]
    public void InvalidOptionsAreRejected(string flag, string value) =>
        Assert.Throws<DemoSimulationException>(() => DemoOptions.Parse([flag, value]));

    [Fact]
    public void DuplicateAndMissingArgumentsAreRejected()
    {
        Assert.Throws<DemoSimulationException>(() => DemoOptions.Parse(["--run-id", "a", "--run-id", "b"]));
        Assert.Throws<DemoSimulationException>(() => DemoOptions.Parse(["--users"]));
    }

    [RbacDatabaseFact]
    public async Task SimulationResumesWithoutDuplicatesAndRejectsChangedPassword()
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("MATCHINGSPORTS_RBAC_TEST_CONNECTION")!)
        { Database = $"matchingsports_rbac_test_demo_resume_{Guid.NewGuid():N}" };
        var directory = Path.Combine(Path.GetTempPath(), $"matchingsports-demo-{Guid.NewGuid():N}");
        var password = Guid.NewGuid().ToString("N") + "Aa!9";
        var configuration = Configuration(connection.ConnectionString, password);
        var options = new DemoOptions(16, 2, 8, "resume-check", OutputDirectory: directory);
        try
        {
            var first = await DemoRunner.RunAsync(configuration, options);
            Assert.Equal(16, first.Users.Count);
            Assert.Equal(2, first.Teams.Count);
            Assert.Equal(8, first.Listings.Count);
            Assert.Equal(1, first.WatchDeliveries);
            Assert.Equal(2, first.ReminderHistories);
            Assert.True(first.Notifications > 0);
            Assert.Equal(10, first.Users.Count(user => user.TeamId.HasValue));
            Assert.Equal(3, first.Listings.Count(listing => listing.Status == "CONFIRMED"));
            Assert.Equal(3, first.Listings.Count(listing => listing.Status == "CANCELLED"));
            var counts = await CountsAsync(connection.ConnectionString);
            var second = await DemoRunner.RunAsync(configuration, options);
            Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
            Assert.Equal(counts, await CountsAsync(connection.ConnectionString));

            // Simulate interruption after the final action committed, before the step checkpoint.
            var statePath = Directory.GetFiles(directory, "state.json", SearchOption.AllDirectories).Single();
            var state = System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(statePath))!;
            state["completed"] = false;
            state["activeStep"] = "watch-matched-and-deduplicated";
            var steps = state["completedSteps"]!.AsArray();
            var step = steps.Single(item => item!.GetValue<string>() == "watch-matched-and-deduplicated");
            steps.Remove(step);
            await File.WriteAllTextAsync(statePath, state.ToJsonString());
            var resumed = await DemoRunner.RunAsync(configuration, options);
            Assert.Equal(1, resumed.WatchDeliveries);
            Assert.Equal(counts, await CountsAsync(connection.ConnectionString));
            await Assert.ThrowsAsync<DemoSimulationException>(() =>
                DemoRunner.RunAsync(Configuration(connection.ConnectionString, password + "different"), options));
            Assert.Equal(counts, await CountsAsync(connection.ConnectionString));
            var report = await File.ReadAllTextAsync(Directory.GetFiles(directory, "report.json", SearchOption.AllDirectories).Single());
            Assert.DoesNotContain(password, report, StringComparison.Ordinal);
            await using var db = new NpgsqlConnection(connection.ConnectionString);
            await db.OpenAsync();
            await using var audit = new NpgsqlCommand("SELECT count(*) FROM teams WHERE created_by = captain_user_id", db);
            Assert.Equal(2L, await audit.ExecuteScalarAsync());
        }
        finally
        {
            await DeleteDatabaseAsync(connection);
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [RbacDatabaseFact]
    public async Task TestModeDeletesItsDatabaseAfterSuccess()
    {
        var raw = Environment.GetEnvironmentVariable("MATCHINGSPORTS_RBAC_TEST_CONNECTION")!;
        var directory = Path.Combine(Path.GetTempPath(), $"matchingsports-demo-{Guid.NewGuid():N}");
        var admin = new NpgsqlConnectionStringBuilder(raw) { Database = "postgres" };
        await using var db = new NpgsqlConnection(admin.ConnectionString);
        await db.OpenAsync();
        var before = await DemoDatabasesAsync(db);
        try
        {
            var report = await DemoRunner.RunAsync(Configuration(raw, Guid.NewGuid().ToString("N") + "Aa!9"),
                new DemoOptions(16, 2, 8, "cleanup-check", TestMode: true, OutputDirectory: directory));
            Assert.True(report.TestDatabase);
            Assert.Equal(before, await DemoDatabasesAsync(db));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [RbacDatabaseFact]
    public async Task TestModeDeletesItsDatabaseWhenIdentityRejectsThePassword()
    {
        var raw = Environment.GetEnvironmentVariable("MATCHINGSPORTS_RBAC_TEST_CONNECTION")!;
        var directory = Path.Combine(Path.GetTempPath(), $"matchingsports-demo-{Guid.NewGuid():N}");
        var admin = new NpgsqlConnectionStringBuilder(raw) { Database = "postgres" };
        await using var db = new NpgsqlConnection(admin.ConnectionString);
        await db.OpenAsync();
        var before = await DemoDatabasesAsync(db);
        try
        {
            await Assert.ThrowsAsync<DemoSimulationException>(() => DemoRunner.RunAsync(Configuration(raw, "too-weak"),
                new DemoOptions(16, 2, 8, "failed-cleanup", TestMode: true, OutputDirectory: directory)));
            Assert.Equal(before, await DemoDatabasesAsync(db));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static IConfiguration Configuration(string connection, string password) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:matchingsports"] = connection, ["Demo:Password"] = password }).Build();

    private static async Task<string> CountsAsync(string connection)
    {
        await using var db = new NpgsqlConnection(connection);
        await db.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT concat_ws(',', (SELECT count(*) FROM users), (SELECT count(*) FROM teams),
                (SELECT count(*) FROM team_members), (SELECT count(*) FROM listings),
                (SELECT count(*) FROM listing_requests), (SELECT count(*) FROM matches),
                (SELECT count(*) FROM notification_history), (SELECT count(*) FROM listing_watch_deliveries))
            """, db);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<string> DemoDatabasesAsync(NpgsqlConnection db)
    {
        await using var command = new NpgsqlCommand("""
            SELECT coalesce(string_agg(datname, ',' ORDER BY datname), '')
            FROM pg_database WHERE datname LIKE 'matchingsports_rbac_test_demo_%'
            """, db);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private static async Task DeleteDatabaseAsync(NpgsqlConnectionStringBuilder connection)
    {
        Assert.StartsWith("matchingsports_rbac_test_demo_resume_", connection.Database, StringComparison.Ordinal);
        var admin = new NpgsqlConnectionStringBuilder(connection.ConnectionString) { Database = "postgres" };
        await using var db = new NpgsqlConnection(admin.ConnectionString);
        await db.OpenAsync();
        await using var targetPool = new NpgsqlConnection(connection.ConnectionString);
        NpgsqlConnection.ClearPool(targetPool);
        await using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{connection.Database}\" WITH (FORCE)", db);
        await command.ExecuteNonQueryAsync();
    }
}
