using MatchingSports.Domain.Enums;

namespace MatchingSports.Application.Features.PublicProfile;

// Public views deliberately omit contact details, account state, permissions and internal audit actors.
public sealed record PublicUserSummaryDto(Guid UserId, string DisplayName, string? AvatarUrl);
public sealed record PublicSportSummaryDto(Guid SportId, string Code, string Name);
public sealed record PublicFormatDto(Guid FormatId, string Code, string DisplayName, short SideSize);
public sealed record PublicSportProfileDto(PublicSportSummaryDto Sport, SportLevel Level, string? Bio,
    IReadOnlyList<PublicFormatDto> PreferredFormats);
public sealed record PublicUserProfileDto(Guid UserId, string DisplayName, string? AvatarUrl, string? DefaultArea,
    IReadOnlyList<PublicSportProfileDto> SportProfiles);
public sealed record PublicTeamProfileDto(Guid TeamId, string Name, string? Description, string? AvatarUrl,
    string? HomeArea, PublicSportSummaryDto Sport, PublicUserSummaryDto Captain, int ActiveMemberCount);
