using MatchingSports.Shared;

namespace MatchingSports.Domain.Entities;

public sealed class UserProfile
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string? DefaultArea { get; set; }
    public string? ContactNote { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;

    public void UpdateDetails(
        string displayName,
        string? avatarUrl,
        string? defaultArea,
        string? contactNote)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        DisplayName = displayName.Trim();
        AvatarUrl = StringNormalizer.StartEndSpaceTrim(avatarUrl);
        DefaultArea = StringNormalizer.StartEndSpaceTrim(defaultArea);
        ContactNote = StringNormalizer.StartEndSpaceTrim(contactNote);
    }
}
