using MatchingSports.Domain.Enums;
using MatchingSports.Domain.ValueObjects;

namespace MatchingSports.Application.Features.Matching;

public sealed record ListingInput(Guid SportId, Guid FormatId, Guid? HostTeamId, ListingMode Mode,
    string Title, string? Note, SportLevel LevelMin, SportLevel LevelMax, short OpenSlots,
    DateTimeOffset StartAt, short DurationMinutes, DateTimeOffset CutoffAt, string AddressText,
    decimal Latitude, decimal Longitude)
{
    internal ListingValues ToDomainValues() => new(SportId, FormatId, HostTeamId, Mode, Title, Note,
        LevelMin, LevelMax, OpenSlots, StartAt, DurationMinutes, CutoffAt, AddressText, Latitude, Longitude);
}
public sealed record ListingWatchInput(Guid SportId, Guid? FormatId, decimal Latitude, decimal Longitude,
    int RadiusMeters, DateTimeOffset StartFrom, DateTimeOffset StartTo)
{
    internal ListingCriteria ToCriteria() => new(SportId, FormatId, ListingMode.FIND_PLAYERS,
        StartFrom: StartFrom, StartTo: StartTo, Latitude: Latitude, Longitude: Longitude, RadiusMeters: RadiusMeters);
}
