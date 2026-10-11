namespace MatchingSports.Api.Contracts;

public sealed class GetRolePermissionMatrixRequest : BaseRequest
{
    public bool? IsActive { get; init; }
}
