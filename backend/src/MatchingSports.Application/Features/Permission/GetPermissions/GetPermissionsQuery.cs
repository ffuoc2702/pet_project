using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Role;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Permission;

public sealed record GetPermissionsQuery : BaseQuery<PermissionDto>, IPermissionAuthorizedRequest
{
    public Guid ActorUserId { get; init; }

    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ListPermissions];
}

public sealed class GetPermissionsQueryHandler(IPermissionRepository permissionRepository, IMapper mapper)
    : IRequestHandler<GetPermissionsQuery, PaginatedResult<PermissionDto>>
{
    public async Task<PaginatedResult<PermissionDto>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissions = await permissionRepository.ListAsync(request, cancellationToken);
        return permissions.Map(mapper.Map<PermissionDto>);
    }
}
