using System.ComponentModel.DataAnnotations;
using MatchingSports.Domain.Enums;

namespace MatchingSports.Api.Contracts;

public sealed class GetUsersRequest : BaseRequest
{
    [EnumDataType(typeof(UserStatus), ErrorMessage = "Trạng thái người dùng không hợp lệ.")]
    public UserStatus? Status { get; init; }
}
