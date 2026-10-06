using System.ComponentModel.DataAnnotations;

namespace MatchingSports.Api.Contracts;

public sealed class CreateRoleRequest
{
    [Required(ErrorMessage = "Trường {0} là bắt buộc."), StringLength(100, ErrorMessage = "Trường {0} không được vượt quá {1} ký tự.")]
    public string Name { get; set; } = null!;
    [StringLength(500, ErrorMessage = "Trường {0} không được vượt quá {1} ký tự.")]
    public string? Description { get; set; }
    public bool IsDefaultForRegistration { get; set; }
}
