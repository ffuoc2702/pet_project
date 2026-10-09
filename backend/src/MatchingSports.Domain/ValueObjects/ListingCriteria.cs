using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;

namespace MatchingSports.Domain.ValueObjects;

public sealed record ListingCriteria(Guid? SportId = null, Guid? FormatId = null, ListingMode? Mode = null,
    SportLevel? Level = null, DateTimeOffset? StartFrom = null, DateTimeOffset? StartTo = null,
    decimal? Latitude = null, decimal? Longitude = null, int? RadiusMeters = null)
{
    public ListingCriteria Normalize() => this with
    {
        StartFrom = StartFrom?.ToUniversalTime(),
        StartTo = StartTo?.ToUniversalTime(),
        Latitude = Latitude is { } latitude ? decimal.Round(latitude, 6) : null,
        Longitude = Longitude is { } longitude ? decimal.Round(longitude, 6) : null
    };
    public void Validate(int maxRadiusMeters)
    {
        MatchingRules.Require(SportId != Guid.Empty && FormatId != Guid.Empty, "Môn và thể thức không hợp lệ.");
        MatchingRules.Require(Mode is null || Enum.IsDefined(Mode.Value), "Chế độ tìm trận không hợp lệ.");
        MatchingRules.Require(Level is null || Enum.IsDefined(Level.Value), "Trình độ không hợp lệ.");
        MatchingRules.Require(StartFrom is null || StartTo is null || StartFrom <= StartTo, "Khoảng thời gian bắt đầu trận không hợp lệ.");
        MatchingRules.Require((Latitude is null && Longitude is null && RadiusMeters is null)
            || (Latitude is >= -90 and <= 90 && Longitude is >= -180 and <= 180 && RadiusMeters is > 0 && RadiusMeters <= maxRadiusMeters),
            "Cần cung cấp đủ tọa độ và bán kính trong giới hạn cấu hình.");
    }
}

public sealed record ListingValues(Guid SportId, Guid FormatId, Guid? HostTeamId, ListingMode Mode,
    string Title, string? Note, SportLevel LevelMin, SportLevel LevelMax, short OpenSlots,
    DateTimeOffset StartAt, short DurationMinutes, DateTimeOffset CutoffAt, string AddressText,
    decimal Latitude, decimal Longitude)
{
    public void Validate(DateTimeOffset now)
    {
        MatchingRules.Require(SportId != Guid.Empty && FormatId != Guid.Empty && HostTeamId != Guid.Empty, "Môn, thể thức hoặc đội không hợp lệ.");
        MatchingRules.Require(Enum.IsDefined(Mode) && Enum.IsDefined(LevelMin) && Enum.IsDefined(LevelMax) && LevelMin <= LevelMax, "Chế độ hoặc khoảng trình độ không hợp lệ.");
        MatchingRules.Require(Mode != ListingMode.FIND_OPPONENT || (HostTeamId is not null && OpenSlots == 1), "Kèo tìm đối thủ phải thuộc đội và chỉ nhận một đội đối thủ.");
        MatchingRules.Require(OpenSlots > 0, "Số chỗ cần tuyển phải lớn hơn 0.");
        MatchingRules.Require(StartAt > now && CutoffAt > now && CutoffAt <= StartAt && DurationMinutes is >= 15 and <= 480, "Thời gian bắt đầu, thời lượng hoặc hạn đăng ký không hợp lệ.");
        MatchingRules.Require(Latitude is >= -90 and <= 90 && Longitude is >= -180 and <= 180, "Tọa độ không hợp lệ.");
        _ = MatchingRules.Text(Title, 160, "Tiêu đề");
        _ = MatchingRules.Text(AddressText, 255, "Địa điểm");
        MatchingRules.Require(Note is null || Note.Length <= 4000, "Ghi chú không được vượt quá 4000 ký tự.");
    }
}
