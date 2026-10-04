using System.ComponentModel.DataAnnotations;

namespace MatchingSports.Api.Contracts;

public sealed class UpdateRoleRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = null!;
    [StringLength(500)]
    public string? Description { get; set; }
    [Required]
    public bool? IsActive { get; set; }
    [Required]
    public bool? IsDefaultForRegistration { get; set; }
    [Required]
    public string ConcurrencyStamp { get; set; } = null!;
}
