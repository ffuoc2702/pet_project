using MatchingSports.Application.Common;

namespace MatchingSports.Application.Features.UserProfile;

public sealed record UserProfileDto : BaseDto
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string? DefaultArea { get; set; }
    public string? ContactNote { get; set; }
}
