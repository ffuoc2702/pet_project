namespace MatchingSports.Api.Contracts;

public sealed class GetSportsRequest : BaseRequest
{
    public bool ActiveOnly { get; init; } = true;
    public string? SportCode { get; init; }
}
