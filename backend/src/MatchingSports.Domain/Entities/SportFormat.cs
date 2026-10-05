using MatchingSports.Domain.ValueObjects;

namespace MatchingSports.Domain.Entities;

public sealed class SportFormat
{
    public Guid Id { get; set; }
    public Guid SportId { get; set; }
    public string Code { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public short SideSize { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public static SportFormat Create(Guid sportId, SportFormatDetails details)
    {
        if (sportId == Guid.Empty)
            throw new ArgumentException("ID môn thể thao là bắt buộc.", nameof(sportId));
        var format = new SportFormat { Id = Guid.NewGuid(), SportId = sportId };
        format.UpdateDetails(details);
        return format;
    }

    public void UpdateDetails(SportFormatDetails details)
    {
        if (details is null)
            throw new ArgumentNullException(nameof(details), "Thông tin chi tiết không được là null.");
        Code = details.Code;
        DisplayName = details.DisplayName;
        SideSize = details.SideSize;
        IsActive = true;
    }

    public void Deactivate() => IsActive = false;

    public Sport Sport { get; set; } = null!;
    public ICollection<Listing> Listings { get; set; } = [];
}
