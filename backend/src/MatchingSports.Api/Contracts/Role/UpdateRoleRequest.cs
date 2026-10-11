using System.ComponentModel.DataAnnotations;
using MatchingSports.Shared;

namespace MatchingSports.Api.Contracts;

public sealed class UpdateRoleRequest
{
    [Required(ErrorMessage = "Trường {0} là bắt buộc."), StringLength(100, ErrorMessage = "Trường {0} không được vượt quá {1} ký tự.")]
    public string Name { get; set; } = null!;
    [StringLength(ResourceLimits.DescriptionMaxLength, ErrorMessage = "Trường {0} không được vượt quá {1} ký tự.")]
    public string? Description { get; set; }
    [Required(ErrorMessage = "Trường {0} là bắt buộc.")]
    public bool? IsActive { get; set; }
    [Required(ErrorMessage = "Trường {0} là bắt buộc.")]
    public bool? IsDefaultForRegistration { get; set; }
    [Required(ErrorMessage = "Trường {0} là bắt buộc.")]
    public string ConcurrencyStamp { get; set; } = null!;
}
