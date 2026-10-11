using System.ComponentModel.DataAnnotations;
using MatchingSports.Domain.Enums;
using MatchingSports.Shared;

namespace MatchingSports.Api.Contracts;

public class TeamDetailsRequest
{
    [Required(ErrorMessage = "Tên đội là bắt buộc."), StringLength(120, ErrorMessage = "Tên đội tối đa 120 ký tự.")] public string Name { get; init; } = string.Empty;
    [StringLength(ResourceLimits.DescriptionMaxLength, ErrorMessage = "Mô tả đội tối đa 200 ký tự.")] public string? Description { get; init; }
    [StringLength(150, ErrorMessage = "Khu vực tối đa 150 ký tự.")] public string? HomeArea { get; init; }
    [StringLength(2048, ErrorMessage = "Đường dẫn ảnh tối đa 2048 ký tự.")] public string? AvatarUrl { get; init; }
}
public sealed class CreateTeamRequest : TeamDetailsRequest
{
    public Guid SportId { get; init; }
}
public sealed class UpdateTeamRequest : TeamDetailsRequest
{
    public Guid ConcurrencyToken { get; init; }
}
public sealed class TeamVersionRequest
{
    public Guid ConcurrencyToken { get; init; }
}
public sealed class TeamUserRequest
{
    public Guid UserId { get; init; }
}
public sealed class TransferTeamCaptainRequest
{
    public Guid UserId { get; init; }
    public Guid ConcurrencyToken { get; init; }
}
public sealed class CreateTeamJoinRequest
{
    [StringLength(1000, ErrorMessage = "Lời nhắn tối đa 1000 ký tự.")] public string? Message { get; init; }
}
public sealed class GetTeamsRequest : BaseRequest
{
    public bool MineOnly { get; init; }
    public Guid? SportId { get; init; }
}
public sealed class GetTeamMembersRequest : BaseRequest;
public sealed class GetTeamInvitationsRequest : BaseRequest
{
    [EnumDataType(typeof(TeamRequestStatus), ErrorMessage = "Trạng thái yêu cầu không hợp lệ.")] public TeamRequestStatus? Status { get; init; }
}
public sealed class GetTeamJoinRequestsRequest : BaseRequest
{
    [EnumDataType(typeof(TeamRequestStatus), ErrorMessage = "Trạng thái yêu cầu không hợp lệ.")] public TeamRequestStatus? Status { get; init; }
}
