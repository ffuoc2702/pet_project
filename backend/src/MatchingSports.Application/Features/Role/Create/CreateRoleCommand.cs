using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Enums;
using MatchingSports.Shared;
using MediatR;
using RoleEntity = MatchingSports.Domain.Entities.Role;

namespace MatchingSports.Application.Features.Role;

public sealed record CreateRoleCommand(Guid ActorUserId, string Name, string? Description,
    bool IsDefaultForRegistration) : IRequest<RoleDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.CreateRoles];
}

public sealed class CreateRoleCommandHandler(IRoleRepository roles, IAdminActionRepository auditRepository, IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<CreateRoleCommand, RoleDto>
{
    public async Task<RoleDto> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = RoleAuthorization.ValidateRoleInput(() => RoleEntity.Create(request.Name, request.Description, request.IsDefaultForRegistration));
        if (await roles.ExistsByNormalizedNameAsync(role.NormalizedName!, null, cancellationToken))
            throw new ConflictException(ErrorCodes.DuplicateResource, "Tên vai trò đã tồn tại.");
        roles.Add(role);
        var result = mapper.Map<RoleDto>(role);
        RoleAuthorization.RecordChange(auditRepository, request.ActorUserId, AdminActionType.CREATE_ROLE, EntityTypes.Role, role.Id, null, result);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.Map<RoleDto>(role);
    }
}
