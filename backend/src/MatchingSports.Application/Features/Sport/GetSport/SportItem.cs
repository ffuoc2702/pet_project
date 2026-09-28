namespace MatchingSports.Application.Features.Sport.GetSport;

public sealed class SportFormatItem
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public short SideSize { get; set; }
}

public sealed class SportItem
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public List<SportFormatItem> Formats { get; set; } = [];
}
