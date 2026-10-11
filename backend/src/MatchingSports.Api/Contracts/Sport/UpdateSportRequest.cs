using System.ComponentModel.DataAnnotations;
using MatchingSports.Shared;

namespace MatchingSports.Api.Contracts;

public sealed class UpdateSportRequest
{
    [Required(ErrorMessage = "Mã môn thể thao là bắt buộc.")]
    [StringLength(32, ErrorMessage = "Mã môn thể thao không được vượt quá 32 ký tự.")]
    public string Code { get; init; } = null!;

    [Required(ErrorMessage = "Tên môn thể thao là bắt buộc.")]
    [StringLength(100, ErrorMessage = "Tên môn thể thao không được vượt quá 100 ký tự.")]
    public string Name { get; init; } = null!;

    [StringLength(ResourceLimits.DescriptionMaxLength, ErrorMessage = "Mô tả không được vượt quá 200 ký tự.")]
    public string? Description { get; init; }
    [Required(ErrorMessage = "Danh sách thể thức là bắt buộc.")]
    [MinLength(1, ErrorMessage = "Phải có ít nhất một thể thức.")]
    public List<SportFormatRequest> Formats { get; init; } = null!;
}
