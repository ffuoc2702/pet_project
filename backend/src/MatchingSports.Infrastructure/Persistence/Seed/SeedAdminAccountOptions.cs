namespace MatchingSports.Infrastructure.Persistence.Seed;

public sealed class SeedAdminAccountOptions
{
    public const string SectionName = "Seed:Admin";
    public bool Enabled { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string DisplayName { get; init; } = "Administrator";
}
