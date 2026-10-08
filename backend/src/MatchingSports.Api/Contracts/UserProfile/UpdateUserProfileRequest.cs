
using System.ComponentModel.DataAnnotations;

namespace MatchingSports.Api.Contracts;

public sealed class UpdateUserProfileRequest
{
    [Required(ErrorMessage = "Tên hiển thị là bắt buộc.")]
    [StringLength(100, ErrorMessage = "Tên hiển thị không được vượt quá 100 ký tự.")]
    public string DisplayName { get; set; } = null!;

    [Url(ErrorMessage = "Địa chỉ ảnh đại diện không hợp lệ.")]
    public string? AvatarUrl { get; set; }

    [StringLength(150, ErrorMessage = "Khu vực mặc định không được vượt quá 150 ký tự.")]
    public string? DefaultArea { get; set; }

    public string? ContactNote { get; set; }
}
