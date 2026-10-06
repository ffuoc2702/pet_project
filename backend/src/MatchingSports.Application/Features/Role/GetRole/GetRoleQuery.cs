using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Role;

public sealed record GetRoleQuery(Guid ActorUserId, Guid RoleId) : IRequest<RoleDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ReadRoles];
}

public sealed class GetRoleQueryHandler(IRoleRepository roles, IMapper mapper)
    : IRequestHandler<GetRoleQuery, RoleDto>
{
    public async Task<RoleDto> Handle(GetRoleQuery request, CancellationToken cancellationToken)
    {
        var role = await roles.GetByIdWithPermissionsAsync(request.RoleId, cancellationToken)
            ?? throw new NotFoundException(ErrorCodes.RoleNotFound, "Không tìm thấy vai trò.");
        return mapper.Map<RoleDto>(role);
    }
}
