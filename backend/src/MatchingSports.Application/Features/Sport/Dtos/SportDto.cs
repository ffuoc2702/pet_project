namespace MatchingSports.Application.Features.Sport.Dtos;

public sealed class SportFormatDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public short SideSize { get; set; }
}

public sealed class SportDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public List<SportFormatDto> Formats { get; set; } = [];
}
