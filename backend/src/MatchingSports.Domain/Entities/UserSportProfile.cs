using MatchingSports.Domain.Enums;

using MatchingSports.Shared;

namespace MatchingSports.Domain.Entities;

public sealed class UserSportProfile : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SportId { get; set; }
    public SportLevel Level { get; set; } = SportLevel.BEGINNER;
    public ICollection<SportFormat> PreferredFormats { get; set; } = [];
    public string? Bio { get; set; }

    public User User { get; set; } = null!;
    public Sport Sport { get; set; } = null!;
    public static UserSportProfile Create(Guid userId, Sport sport, string level,
        IReadOnlyCollection<Guid> preferredFormatIds, string? bio)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("ID người dùng là bắt buộc.", nameof(userId));
        ArgumentNullException.ThrowIfNull(sport);
        if (sport.Id == Guid.Empty || !sport.IsActive)
            throw new ArgumentException("Môn thể thao phải đang hoạt động.", nameof(sport));

        var profile = new UserSportProfile { Id = Guid.NewGuid(), UserId = userId, SportId = sport.Id, Sport = sport };
        profile.UpdateDetails(level, preferredFormatIds, bio);
        return profile;
    }

    public void UpdateDetails(string level, IReadOnlyCollection<Guid> preferredFormatIds, string? bio)
    {
        if (!Enum.GetNames<SportLevel>().Contains(level, StringComparer.Ordinal))
            throw new ArgumentException("Trình độ không hợp lệ.", nameof(level));
        if (Sport is null || Sport.Id != SportId || !Sport.IsActive)
            throw new ArgumentException("Môn thể thao phải đang hoạt động.");
        if (preferredFormatIds is null)
            throw new ArgumentNullException(nameof(preferredFormatIds), "Danh sách thể thức không được là null.");
        if (preferredFormatIds.Distinct().Count() != preferredFormatIds.Count)
            throw new ArgumentException("Danh sách thể thức không được trùng lặp.", nameof(preferredFormatIds));
        var formats = preferredFormatIds.Select(id => Sport.Formats.SingleOrDefault(format =>
                format.Id == id && id != Guid.Empty && format.SportId == SportId && format.IsActive)
            ?? throw new ArgumentException("Thể thức phải đang hoạt động và thuộc môn thể thao của hồ sơ.", nameof(preferredFormatIds))).ToArray();

        // Validate the whole selection before changing existing links.
        Level = Enum.Parse<SportLevel>(level);
        Bio = StringNormalizer.StartEndSpaceTrim(bio);
        foreach (var format in PreferredFormats.Where(format => !formats.Any(selected => selected.Id == format.Id)).ToArray())
            PreferredFormats.Remove(format);
        foreach (var format in formats.Where(format => !PreferredFormats.Any(existing => existing.Id == format.Id)))
            PreferredFormats.Add(format);
    }
}
