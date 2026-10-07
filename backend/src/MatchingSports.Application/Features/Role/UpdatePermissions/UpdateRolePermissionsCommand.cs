using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Enums;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Role;

public sealed record UpdateRolePermissionsCommand(Guid ActorUserId, Guid RoleId, IReadOnlyCollection<string> PermissionCodes, string ConcurrencyStamp)
    : IRequest<RoleDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [MatchingSports.Shared.PermissionCodes.UpdateRolePermissions];
}

public sealed class UpdateRolePermissionsCommandHandler(
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository,
    IAdminActionRepository audit,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<UpdateRolePermissionsCommand, RoleDto>
{
    public async Task<RoleDto> Handle(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetForUpdateAsync(request.RoleId, cancellationToken)
            ?? throw new NotFoundException(ErrorCodes.RoleNotFound, "Không tìm thấy vai trò.");
        RoleAuthorization.RequireCurrentVersion(role.ConcurrencyStamp, request.ConcurrencyStamp);
        if (request.PermissionCodes is null || request.PermissionCodes.Any(string.IsNullOrWhiteSpace))
            throw InvalidPermissions(["Danh sách mã quyền không hợp lệ."]);

        var codes = request.PermissionCodes.Distinct(StringComparer.Ordinal).ToArray();
        var permissions = await permissionRepository.GetByCodesAsync(codes, cancellationToken);
        var unknownCodes = codes.Except(permissions.Select(permission => permission.Code), StringComparer.Ordinal).ToArray();
        if (unknownCodes.Length > 0)
            throw InvalidPermissions(unknownCodes);

        var before = mapper.Map<RoleDto>(role);
        role.ReplacePermissions(permissions);
        var result = mapper.Map<RoleDto>(role);
        RoleAuthorization.RecordChange(audit, request.ActorUserId, AdminActionType.UPDATE_ROLE_PERMISSIONS, EntityTypes.Role,
            role.Id, before, result);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }

    private static RequestValidationException InvalidPermissions(string[] errors) =>
        new(ErrorCodes.InvalidPermission, "Quyền không tồn tại hoặc không hợp lệ.",
            new Dictionary<string, string[]> { ["permissionCodes"] = errors });
}
