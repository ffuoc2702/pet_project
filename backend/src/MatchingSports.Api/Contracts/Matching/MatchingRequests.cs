using System.ComponentModel.DataAnnotations;
using MatchingSports.Domain.Enums;
using MatchingSports.Shared;

namespace MatchingSports.Api.Contracts;

public class ListingDetailsRequest
{
    [Required(ErrorMessage = "Môn thể thao là bắt buộc.")]
    public Guid? SportId { get; init; }
    [Required(ErrorMessage = "Thể thức là bắt buộc.")]
    public Guid? FormatId { get; init; }
    public Guid? HostTeamId { get; init; }
    [Required(ErrorMessage = "Chế độ tìm trận là bắt buộc."), EnumDataType(typeof(ListingMode), ErrorMessage = "Chế độ tìm trận không hợp lệ.")]
    public ListingMode? Mode { get; init; }
    [Required(ErrorMessage = "Tiêu đề là bắt buộc."), StringLength(160, ErrorMessage = "Tiêu đề không được vượt quá 160 ký tự.")]
    public string Title { get; init; } = null!;
    [StringLength(ResourceLimits.DescriptionMaxLength, ErrorMessage = "Mô tả không được vượt quá 200 ký tự.")]
    public string? Description { get; init; }
    [StringLength(4000, ErrorMessage = "Ghi chú không được vượt quá 4000 ký tự.")]
    public string? Note { get; init; }
    [EnumDataType(typeof(SportLevel), ErrorMessage = "Trình độ không hợp lệ.")]
    public SportLevel LevelMin { get; init; } = SportLevel.BEGINNER;
    [EnumDataType(typeof(SportLevel), ErrorMessage = "Trình độ không hợp lệ.")]
    public SportLevel LevelMax { get; init; } = SportLevel.ADVANCED;
    [Required(ErrorMessage = "Số chỗ là bắt buộc."), Range(1, 32767, ErrorMessage = "Số chỗ phải từ 1 đến 32767.")]
    public short? OpenSlots { get; init; }
    [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc.")]
    public DateTimeOffset? StartAt { get; init; }
    [Required(ErrorMessage = "Thời lượng là bắt buộc."), Range(15, 480, ErrorMessage = "Thời lượng phải từ 15 đến 480 phút.")]
    public short? DurationMinutes { get; init; }
    [Required(ErrorMessage = "Hạn đăng ký là bắt buộc.")]
    public DateTimeOffset? CutoffAt { get; init; }
    [Required(ErrorMessage = "Địa điểm là bắt buộc."), StringLength(255, ErrorMessage = "Địa điểm không được vượt quá 255 ký tự.")]
    public string AddressText { get; init; } = null!;
    [Required(ErrorMessage = "Vĩ độ là bắt buộc."), Range(typeof(decimal), "-90", "90", ErrorMessage = "Vĩ độ phải từ -90 đến 90.")]
    public decimal? Latitude { get; init; }
    [Required(ErrorMessage = "Kinh độ là bắt buộc."), Range(typeof(decimal), "-180", "180", ErrorMessage = "Kinh độ phải từ -180 đến 180.")]
    public decimal? Longitude { get; init; }
}
public sealed class CreateListingRequest : ListingDetailsRequest;
public sealed class UpdateListingRequest : ListingDetailsRequest
{
    [Required(ErrorMessage = "Phiên bản kèo là bắt buộc.")]
    public Guid? ConcurrencyToken { get; init; }
}
public class MatchingVersionRequest
{
    [Required(ErrorMessage = "Phiên bản dữ liệu là bắt buộc.")]
    public Guid? ConcurrencyToken { get; init; }
}
public sealed class MatchingReasonRequest : MatchingVersionRequest
{
    [Required(ErrorMessage = "Lý do là bắt buộc."), StringLength(1000, ErrorMessage = "Lý do không được vượt quá 1000 ký tự.")]
    public string Reason { get; init; } = null!;
}
public sealed class JoinListingRequest
{
    public Guid? ApplicantTeamId { get; init; }
    [StringLength(1000, ErrorMessage = "Lời nhắn không được vượt quá 1000 ký tự.")]
    public string? Message { get; init; }
}
public sealed class GetListingsRequest : BaseRequest
{
    public Guid? SportId { get; init; }
    public Guid? FormatId { get; init; }
    [EnumDataType(typeof(ListingMode), ErrorMessage = "Chế độ tìm trận không hợp lệ.")]
    public ListingMode? Mode { get; init; }
    [EnumDataType(typeof(SportLevel), ErrorMessage = "Trình độ không hợp lệ.")]
    public SportLevel? Level { get; init; }
    public DateTimeOffset? StartFrom { get; init; }
    public DateTimeOffset? StartTo { get; init; }
    [Range(typeof(decimal), "-90", "90", ErrorMessage = "Vĩ độ phải từ -90 đến 90.")]
    public decimal? Latitude { get; init; }
    [Range(typeof(decimal), "-180", "180", ErrorMessage = "Kinh độ phải từ -180 đến 180.")]
    public decimal? Longitude { get; init; }
    [Range(1, 200000, ErrorMessage = "Bán kính phải từ 1 đến 200000 mét.")]
    public int? RadiusMeters { get; init; }
    public bool MineOnly { get; init; }
    [EnumDataType(typeof(ListingStatus), ErrorMessage = "Trạng thái kèo không hợp lệ.")]
    public ListingStatus? Status { get; init; }
}
public sealed class GetListingRequestsRequest : BaseRequest
{
    [EnumDataType(typeof(RequestStatus), ErrorMessage = "Trạng thái yêu cầu không hợp lệ.")]
    public RequestStatus? Status { get; init; }
}
public sealed class GetMatchesRequest : BaseRequest
{
    public Guid? TeamId { get; init; }
    [EnumDataType(typeof(MatchStatus), ErrorMessage = "Trạng thái trận không hợp lệ.")]
    public MatchStatus? Status { get; init; }
    public DateTimeOffset? StartFrom { get; init; }
    public DateTimeOffset? StartTo { get; init; }
}
public class ListingWatchRequest
{
    [Required(ErrorMessage = "Môn thể thao là bắt buộc.")]
    public Guid? SportId { get; init; }
    public Guid? FormatId { get; init; }
    [Required(ErrorMessage = "Vĩ độ là bắt buộc."), Range(typeof(decimal), "-90", "90", ErrorMessage = "Vĩ độ phải từ -90 đến 90.")]
    public decimal? Latitude { get; init; }
    [Required(ErrorMessage = "Kinh độ là bắt buộc."), Range(typeof(decimal), "-180", "180", ErrorMessage = "Kinh độ phải từ -180 đến 180.")]
    public decimal? Longitude { get; init; }
    [Required(ErrorMessage = "Bán kính là bắt buộc."), Range(1, 200000, ErrorMessage = "Bán kính phải từ 1 đến 200000 mét.")]
    public int? RadiusMeters { get; init; }
    [Required(ErrorMessage = "Thời gian bắt đầu khoảng tìm kiếm là bắt buộc.")]
    public DateTimeOffset? StartFrom { get; init; }
    [Required(ErrorMessage = "Thời gian kết thúc khoảng tìm kiếm là bắt buộc.")]
    public DateTimeOffset? StartTo { get; init; }
    public bool EmailEnabled { get; init; }
}
public sealed class UpdateListingWatchRequest : ListingWatchRequest
{
    [Required(ErrorMessage = "Phiên bản theo dõi là bắt buộc.")]
    public Guid? ConcurrencyToken { get; init; }
}
public sealed class GetListingWatchesRequest : BaseRequest
{
    public Guid? SportId { get; init; }
    [EnumDataType(typeof(ListingWatchStatus), ErrorMessage = "Trạng thái theo dõi không hợp lệ.")]
    public ListingWatchStatus? Status { get; init; }
}
