namespace MatchingSports.Api.Contracts;

public sealed class GetSportsRequest : BaseRequest
{
    public bool? IsActive { get; init; }
    public string? SportCode { get; init; }
}
