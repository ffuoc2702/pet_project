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

    public Sport Sport { get; set; } = null!;
    public ICollection<Listing> Listings { get; set; } = [];
}
