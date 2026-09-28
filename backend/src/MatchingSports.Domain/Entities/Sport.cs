namespace MatchingSports.Domain.Entities;

public sealed class Sport
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<SportFormat> Formats { get; set; } = [];
    public ICollection<UserSportProfile> UserSportProfiles { get; set; } = [];
    public ICollection<Team> Teams { get; set; } = [];
    public ICollection<Listing> Listings { get; set; } = [];
}
