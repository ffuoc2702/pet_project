using System.ComponentModel.DataAnnotations;

namespace MatchingSports.Api.Contracts;

public sealed class UpdateRolePermissionsRequest
{
    [Required]
    public string[] PermissionCodes { get; set; } = null!;

    [Required]
    public string ConcurrencyStamp { get; set; } = null!;
}
