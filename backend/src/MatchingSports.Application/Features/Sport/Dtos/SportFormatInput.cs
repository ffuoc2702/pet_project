using MatchingSports.Domain.ValueObjects;

namespace MatchingSports.Application.Features.Sport;

public sealed record SportFormatInput(Guid? Id, string? Code, string? DisplayName, short SideSize, string Action, string? Description = null)
{
    internal static SportFormatDetails[] ToDetails(IReadOnlyCollection<SportFormatInput> formats)
    {
        if (formats is null)
            throw new ArgumentNullException(nameof(formats), "Danh sách thể thức không được là null.");
        return formats.Select(format => format?.ToDetail()
            ?? throw new ArgumentException("Thể thức không được là null.", nameof(formats))).ToArray();
    }

    internal SportFormatDetails ToDetail() => SportFormatDetails.FromAction(Action, Id, Code, DisplayName, SideSize, Description);
}
