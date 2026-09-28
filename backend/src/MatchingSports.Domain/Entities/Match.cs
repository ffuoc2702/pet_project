using MatchingSports.Domain.Enums;

namespace MatchingSports.Domain.Entities;

public sealed class Match : IHasConcurrencyToken
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public MatchStatus Status { get; set; } = MatchStatus.PENDING;
    public DateTimeOffset StartAt { get; set; }
    public short DurationMinutes { get; set; }
    public string AddressText { get; set; } = null!;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public string? CancelReason { get; set; }
    public Guid ConcurrencyToken { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Listing Listing { get; set; } = null!;
    public User? CancelledByUser { get; set; }
    public ICollection<MatchParticipant> Participants { get; set; } = [];
}
