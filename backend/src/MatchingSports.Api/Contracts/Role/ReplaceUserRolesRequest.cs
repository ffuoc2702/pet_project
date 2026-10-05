using System.ComponentModel.DataAnnotations;

namespace MatchingSports.Api.Contracts;

public sealed class ReplaceUserRolesRequest
{
    [Required(ErrorMessage = "Trường {0} là bắt buộc.")]
    public Guid[] RoleIds { get; set; } = null!;

    [Required(ErrorMessage = "Trường {0} là bắt buộc.")]
    public string ConcurrencyStamp { get; set; } = null!;
}
