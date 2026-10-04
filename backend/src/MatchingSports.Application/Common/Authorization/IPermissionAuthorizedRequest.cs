namespace MatchingSports.Application.Common;

public interface IPermissionAuthorizedRequest
{
    Guid ActorUserId { get; }
    IReadOnlyCollection<string> RequiredPermissions { get; }
}
