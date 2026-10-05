using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Role;

public sealed record GetUserRolesQuery(Guid ActorUserId, Guid UserId) : IRequest<UserRoleDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ReadUsers];
}

public sealed class GetUserRolesQueryHandler(IUserRoleRepository userRoleRepository) : IRequestHandler<GetUserRolesQuery, UserRoleDto>
{
    public async Task<UserRoleDto> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
    {
        var assignment = await userRoleRepository.GetUserRolesAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(ErrorCodes.UserNotFound, "Không tìm thấy người dùng.");
        return new UserRoleDto(assignment.UserId, assignment.Roles, assignment.RoleIds, assignment.ConcurrencyStamp);
    }
}
