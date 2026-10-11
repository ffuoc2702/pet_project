using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Role;

public sealed record GetRolePermissionMatrixQuery : BaseQuery, IRequest<RolePermissionMatrixDto>, IPermissionAuthorizedRequest
{
    public Guid ActorUserId { get; init; }
    public bool? IsActive { get; init; }

    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ListRoles, PermissionCodes.ListPermissions];
}

public sealed class GetRolePermissionMatrixQueryHandler(IRoleRepository roles, IPermissionRepository permissions,
    IMapper mapper) : IRequestHandler<GetRolePermissionMatrixQuery, RolePermissionMatrixDto>
{
    public async Task<RolePermissionMatrixDto> Handle(GetRolePermissionMatrixQuery request, CancellationToken cancellationToken)
    {
        var roleQuery = mapper.Map<GetRolesQuery>(request);
        var rolePage = await roles.ListAsync(roleQuery, cancellationToken);
        var allPermissions = await permissions.GetPermissionsAsync(cancellationToken);
        return new RolePermissionMatrixDto(mapper.Map<IReadOnlyCollection<PermissionDto>>(allPermissions),
            rolePage.Map(mapper.Map<RoleDto>));
    }
}
