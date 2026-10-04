using System.ComponentModel.DataAnnotations;

namespace MatchingSports.Api.Contracts;

public sealed class ReplaceUserRolesRequest
{
    [Required]
    public Guid[] RoleIds { get; set; } = null!;

    [Required]
    public string ConcurrencyStamp { get; set; } = null!;
}
