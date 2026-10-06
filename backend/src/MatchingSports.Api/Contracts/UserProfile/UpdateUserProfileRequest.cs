
using System.ComponentModel.DataAnnotations;

namespace MatchingSports.Api.Contracts;

public sealed class UpdateUserProfileRequest
{
    [Required]
    [StringLength(100)]
    public string DisplayName { get; set; } = null!;

    [Url]
    public string? AvatarUrl { get; set; }

    [StringLength(150)]
    public string? DefaultArea { get; set; }

    public string? ContactNote { get; set; }
}
