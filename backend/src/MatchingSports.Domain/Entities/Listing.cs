using MatchingSports.Domain.Enums;

namespace MatchingSports.Domain.Entities;

public sealed class Listing : ConcurrentAuditableEntity
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid? HostTeamId { get; set; }
    public Guid SportId { get; set; }
    public Guid FormatId { get; set; }
    public ListingMode Mode { get; set; }
    public string Title { get; set; } = null!;
    public string? Note { get; set; }
    public SportLevel LevelMin { get; set; } = SportLevel.BEGINNER;
    public SportLevel LevelMax { get; set; } = SportLevel.ADVANCED;
    public short OpenSlots { get; set; }
    public short AcceptedSlots { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public short DurationMinutes { get; set; }
    public DateTimeOffset CutoffAt { get; set; }
    public string AddressText { get; set; } = null!;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public ListingStatus Status { get; set; } = ListingStatus.OPEN;
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public User OwnerUser { get; set; } = null!;
    public Team? HostTeam { get; set; }
    public Sport Sport { get; set; } = null!;
    public SportFormat Format { get; set; } = null!;
    public ICollection<ListingRequest> Requests { get; set; } = [];
    public Match? Match { get; set; }
}
