using MatchingSports.Application.Abstractions;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Common;

public sealed class PermissionAuthorizationBehavior<TRequest, TResponse>(IUserRoleRepository userRoleRepository)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IPermissionAuthorizedRequest
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var permissions = request.RequiredPermissions;
        if (permissions.Count == 0 || permissions.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Yêu cầu có kiểm tra phân quyền phải khai báo đầy đủ các quyền cần thiết.");

        var actor = await userRoleRepository.GetUserRolesAsync(request.ActorUserId, cancellationToken);
        if (actor is null || !permissions.All(permission => actor.HasPermission(permission)))
            throw new ForbiddenException(ErrorCodes.Forbidden, "Bạn không có quyền thực hiện thao tác này.");

        return await next(cancellationToken);
    }
}
