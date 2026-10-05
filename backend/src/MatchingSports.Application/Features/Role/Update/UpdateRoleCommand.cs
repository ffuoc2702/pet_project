using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.ValueObjects;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Role;

public sealed record UpdateRoleCommand(Guid ActorUserId, Guid RoleId, string Name, string? Description,
    bool IsActive, bool IsDefaultForRegistration, string ConcurrencyStamp) : IRequest<RoleDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.UpdateRoles];
}

public sealed class UpdateRoleCommandHandler(IRoleRepository roles, IAuthorizationAuditRepository audit, IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<UpdateRoleCommand, RoleDto>
{
    public async Task<RoleDto> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await roles.GetForUpdateAsync(request.RoleId, cancellationToken)
            ?? throw new NotFoundException(ErrorCodes.RoleNotFound, "Không tìm thấy role.");
        RoleAuthorization.RequireCurrentVersion(role.ConcurrencyStamp, request.ConcurrencyStamp);
        var details = RoleAuthorization.ValidateRoleInput(() => RoleDetails.Create(request.Name, request.Description,
            request.IsActive, request.IsDefaultForRegistration));
        if (await roles.ExistsByNormalizedNameAsync(details.Name, role.Id, cancellationToken))
            throw new ConflictException(ErrorCodes.DuplicateResource, "Tên role đã tồn tại.");
        var before = mapper.Map<RoleDto>(role);
        role.UpdateDetails(details);
        var result = mapper.Map<RoleDto>(role);
        RoleAuthorization.RecordChange(audit, request.ActorUserId, PermissionCodes.UpdateRoles, role.Id, before, result);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }
}
