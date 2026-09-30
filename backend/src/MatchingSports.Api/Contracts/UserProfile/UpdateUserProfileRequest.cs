
using System.ComponentModel.DataAnnotations;

namespace MatchingSports.Api.Contracts;
public sealed class UpdateUserProfileRequest
{
    [Required]
    public Guid UserId { get; set; }
    public string? DisplayName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string? DefaultArea { get; set; }
    public string? ContactNote { get; set; }
}