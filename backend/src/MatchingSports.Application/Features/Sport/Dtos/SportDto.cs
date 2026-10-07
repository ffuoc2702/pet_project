using MatchingSports.Application.Common;

namespace MatchingSports.Application.Features.Sport;

public sealed record SportFormatDto : BaseDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public bool IsActive { get; set; }
    public short SideSize { get; set; }
}

public sealed record SportDto : BaseDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }
    public List<SportFormatDto> Formats { get; set; } = [];
}
