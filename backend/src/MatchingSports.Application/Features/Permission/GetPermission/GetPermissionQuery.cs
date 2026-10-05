using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Role;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Permission;

public sealed record GetPermissionQuery(Guid ActorUserId, Guid PermissionId) : IRequest<PermissionDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ReadPermissions];
}

public sealed class GetPermissionQueryHandler(IPermissionRepository permissions, IMapper mapper)
    : IRequestHandler<GetPermissionQuery, PermissionDto>
{
    public async Task<PermissionDto> Handle(GetPermissionQuery request, CancellationToken cancellationToken)
    {
        var permission = await permissions.GetByIdAsync(request.PermissionId, cancellationToken)
            ?? throw new NotFoundException(ErrorCodes.PermissionNotFound, "Không tìm thấy quyền.");
        return mapper.Map<PermissionDto>(permission);
    }
}
