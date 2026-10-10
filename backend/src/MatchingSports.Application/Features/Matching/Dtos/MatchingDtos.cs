using MatchingSports.Application.Common;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;

namespace MatchingSports.Application.Features.Matching;

public sealed record ListingDto : ConcurrentBaseDto
{
    public Guid Id { get; init; }
    public Guid OwnerUserId { get; init; }
    public Guid? HostTeamId { get; init; }
    public Guid SportId { get; init; }
    public Guid FormatId { get; init; }
    public ListingMode Mode { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Note { get; init; }
    public SportLevel LevelMin { get; init; }
    public SportLevel LevelMax { get; init; }
    public short OpenSlots { get; init; }
    public short AcceptedSlots { get; init; }
    public DateTimeOffset StartAt { get; init; }
    public DateTimeOffset CutoffAt { get; init; }
    public short DurationMinutes { get; init; }
    public string AddressText { get; init; } = string.Empty;
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public ListingStatus Status { get; init; }
    public DateTimeOffset? CancelledAt { get; init; }
    public string? CancelReason { get; init; }
    public Guid? MatchId { get; init; }
    public double? DistanceMeters { get; init; }
}
public sealed record ListingSearchResult(Listing Listing, double? DistanceMeters);
public sealed record ListingRequestDto : ConcurrentBaseDto
{
    public Guid Id { get; init; }
    public Guid ListingId { get; init; }
    public Guid? ApplicantUserId { get; init; }
    public Guid? ApplicantTeamId { get; init; }
    public RequestType Type { get; init; }
    public short RequestedSlots { get; init; }
    public string? Message { get; init; }
    public RequestStatus Status { get; init; }
    public Guid? DecidedByUserId { get; init; }
    public DateTimeOffset? DecidedAt { get; init; }
    public DateTimeOffset? WithdrawnAt { get; init; }
    public string? RejectionReason { get; init; }
}
public sealed record MatchParticipantDto : BaseDto
{
    public Guid Id { get; init; }
    public Guid MatchId { get; init; }
    public Guid? UserId { get; init; }
    public Guid? TeamId { get; init; }
    public Guid? SourceRequestId { get; init; }
    public short SideNo { get; init; }
    public ParticipantRole Role { get; init; }
    public ParticipantStatus Status { get; init; }
}
public sealed record MatchDto : ConcurrentBaseDto
{
    public Guid Id { get; init; }
    public Guid ListingId { get; init; }
    public MatchStatus Status { get; init; }
    public DateTimeOffset StartAt { get; init; }
    public short DurationMinutes { get; init; }
    public string AddressText { get; init; } = string.Empty;
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public DateTimeOffset? ConfirmedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DateTimeOffset? CancelledAt { get; init; }
    public Guid? CancelledByUserId { get; init; }
    public string? CancelReason { get; init; }
    public IReadOnlyList<MatchParticipantDto> Participants { get; init; } = [];
}
public sealed record ListingWatchDto : ConcurrentBaseDto
{
    public Guid Id { get; init; }
    public Guid SportId { get; init; }
    public Guid? FormatId { get; init; }
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public int RadiusMeters { get; init; }
    public DateTimeOffset StartFrom { get; init; }
    public DateTimeOffset StartTo { get; init; }
    public ListingWatchStatus Status { get; init; }
    public bool EmailEnabled { get; init; }
}
public sealed record MatchContactDto(Guid UserId, string DisplayName, string? ContactNote);
