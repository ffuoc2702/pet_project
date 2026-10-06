using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Sport;
using MatchingSports.Domain.Enums;

namespace MatchingSports.Application.Features.UserSportProfile;

public sealed class UserSportProfileDto : BaseDto
{
    public Guid Id { get; set; }
    public Guid SportId { get; set; }
    public string SportCode { get; set; } = null!;
    public string SportName { get; set; } = null!;
    public SportLevel Level { get; set; }
    public string? Bio { get; set; }
    public List<SportFormatDto> PreferredFormats { get; set; } = [];
}
