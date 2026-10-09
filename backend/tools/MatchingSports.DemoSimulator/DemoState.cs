using System.Text.Json;
using System.Text.Json.Serialization;

namespace MatchingSports.DemoSimulator;

internal sealed class DemoState
{
    public int SchemaVersion { get; set; } = 1;
    public string RunId { get; set; } = "";
    public string DatabaseKey { get; set; } = "";
    public int UserCount { get; set; }
    public int TeamCount { get; set; }
    public int ListingCount { get; set; }
    public DateTimeOffset AnchorAt { get; set; }
    public bool Completed { get; set; }
    public Dictionary<string, Guid> Resources { get; set; } = [];
    public Dictionary<string, DateTimeOffset> Schedules { get; set; } = [];
    public HashSet<string> CompletedSteps { get; set; } = [];
    public string? ActiveStep { get; set; }
}

public sealed record DemoUser(int Number, Guid Id, string Email, Guid? TeamId);
public sealed record DemoTeam(Guid Id, string Name, Guid CaptainUserId, Guid DeputyUserId);
public sealed record DemoListing(Guid Id, string Title, string Status, Guid? MatchId, DateTimeOffset StartAt);
public sealed record DemoReport(string RunId, DateTimeOffset CompletedAt, bool TestDatabase,
    IReadOnlyList<DemoUser> Users, IReadOnlyList<DemoTeam> Teams, IReadOnlyList<DemoListing> Listings,
    IReadOnlyList<string> CompletedSteps, long Notifications, int WatchDeliveries, int ReminderHistories);

internal sealed class DemoCheckpoint(string directory)
{
    internal static JsonSerializerOptions Json { get; } = new(JsonSerializerDefaults.Web)
    { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
    public string StatePath => Path.Combine(directory, "state.json");
    public string ReportPath => Path.Combine(directory, "report.json");

    public async Task<DemoState> LoadAsync(DemoOptions options, string databaseKey, CancellationToken token)
    {
        Directory.CreateDirectory(directory);
        if (File.Exists(StatePath))
        {
            var state = JsonSerializer.Deserialize<DemoState>(await File.ReadAllTextAsync(StatePath, token), Json)
                ?? throw new DemoSimulationException("Checkpoint không hợp lệ.");
            if (state.SchemaVersion != 1 || state.RunId != options.RunId || state.DatabaseKey != databaseKey
                || state.UserCount != options.Users || state.TeamCount != options.Teams || state.ListingCount != options.Listings)
                throw new DemoSimulationException("Checkpoint không khớp database/tham số; hãy dùng RunId mới.");
            return state;
        }
        var created = new DemoState
        {
            RunId = options.RunId,
            DatabaseKey = databaseKey,
            UserCount = options.Users,
            TeamCount = options.Teams,
            ListingCount = options.Listings,
            AnchorAt = DateTimeOffset.UtcNow
        };
        await SaveAsync(created, token);
        return created;
    }

    public Task SaveAsync(DemoState state, CancellationToken token) => WriteAsync(StatePath, state, token);
    public Task SaveReportAsync(DemoReport report, CancellationToken token) => WriteAsync(ReportPath, report, token);
    private static async Task WriteAsync<T>(string path, T value, CancellationToken token)
    {
        var temporary = path + ".tmp";
        await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(value, Json), token);
        File.Move(temporary, path, overwrite: true);
    }
}
