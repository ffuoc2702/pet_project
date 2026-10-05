using System.ComponentModel.DataAnnotations;

namespace MatchingSports.Api.Contracts;

public sealed class CreateRoleRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = null!;
    [StringLength(500)]
    public string? Description { get; set; }
    public bool IsDefaultForRegistration { get; set; }
}
