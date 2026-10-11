using System.ComponentModel.DataAnnotations;
using MatchingSports.Shared;

namespace MatchingSports.Api.Contracts;

public sealed class CreateRoleRequest
{
    [Required(ErrorMessage = "Trường {0} là bắt buộc."), StringLength(100, ErrorMessage = "Trường {0} không được vượt quá {1} ký tự.")]
    public string Name { get; set; } = null!;
    [StringLength(ResourceLimits.DescriptionMaxLength, ErrorMessage = "Trường {0} không được vượt quá {1} ký tự.")]
    public string? Description { get; set; }
    public bool IsDefaultForRegistration { get; set; }
}
