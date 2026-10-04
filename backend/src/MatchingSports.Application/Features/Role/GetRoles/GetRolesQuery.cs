using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Role;

public sealed record GetRolesQuery : BaseQuery<RoleDto>, IPermissionAuthorizedRequest
{
    public Guid ActorUserId { get; init; }

    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ListRoles];
}

public sealed class GetRolesQueryHandler(IRoleRepository roleRepository, IMapper mapper)
    : IRequestHandler<GetRolesQuery, PaginatedResult<RoleDto>>
{
    public async Task<PaginatedResult<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await roleRepository.ListAsync(request, cancellationToken);
        return roles.Map(mapper.Map<RoleDto>);
    }
}
