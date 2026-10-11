namespace MatchingSports.Api.Contracts;

public sealed class GetRolesRequest : BaseRequest
{
    public bool? IsActive { get; init; }
}
