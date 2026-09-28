using MatchingSports.Api.Contracts.Common;

namespace MatchingSports.Api.Contracts.Sport;

public sealed class GetSportsRequest : BaseRequest
{
    public bool ActiveOnly { get; init; } = true;
    public string? SportCode { get; init; }
}
