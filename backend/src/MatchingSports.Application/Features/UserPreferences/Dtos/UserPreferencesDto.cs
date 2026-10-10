using MatchingSports.Application.Common;

namespace MatchingSports.Application.Features.UserPreferences;

public sealed record UserPreferencesDto : ConcurrentBaseDto
{
    public string LanguageCode { get; init; } = "vi";
    public string ColorMode { get; init; } = "system";
    public string ThemePreset { get; init; } = "blue";
}
