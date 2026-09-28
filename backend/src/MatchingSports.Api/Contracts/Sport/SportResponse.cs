namespace MatchingSports.Api.Contracts.Sport;

public sealed class SportFormatResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public short SideSize { get; set; }
}

public sealed class SportResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public List<SportFormatResponse> Formats { get; set; } = [];
}
