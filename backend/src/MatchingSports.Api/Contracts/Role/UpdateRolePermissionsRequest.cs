using System.ComponentModel.DataAnnotations;

namespace MatchingSports.Api.Contracts;

public sealed class UpdateRolePermissionsRequest
{
    [Required(ErrorMessage = "Trường {0} là bắt buộc.")]
    public string[] PermissionCodes { get; set; } = null!;

    [Required(ErrorMessage = "Trường {0} là bắt buộc.")]
    public string ConcurrencyStamp { get; set; } = null!;
}
