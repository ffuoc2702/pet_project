using MatchingSports.Domain.Enums;

namespace MatchingSports.Domain.Entities;

public sealed class UserSportProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SportId { get; set; }
    public SportLevel Level { get; set; } = SportLevel.BEGINNER;
    public ICollection<SportFormat> PreferredFormats { get; set; } = [];
    public string? Bio { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Sport Sport { get; set; } = null!;
}
