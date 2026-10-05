using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Entities;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Role;

public sealed record ReplaceUserRolesCommand(Guid ActorUserId, Guid UserId, IReadOnlyCollection<Guid> RoleIds, string ConcurrencyStamp)
    : IRequest<UserRoleDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.AssignUserRole];
}

public sealed class ReplaceUserRolesCommandHandler(
    IUserRoleRepository repository,
    IRoleRepository roleRepository,
    IAuthorizationAuditRepository audit,
    IUnitOfWork unitOfWork) : IRequestHandler<ReplaceUserRolesCommand, UserRoleDto>
{
    public async Task<UserRoleDto> Handle(ReplaceUserRolesCommand request, CancellationToken cancellationToken)
    {
        var assignment = await repository.GetUserRolesAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(ErrorCodes.UserNotFound, "Không tìm thấy người dùng.");
        RoleAuthorization.RequireCurrentVersion(assignment.ConcurrencyStamp, request.ConcurrencyStamp);
        if (request.RoleIds is null || request.RoleIds.Any(id => id == Guid.Empty))
            throw InvalidRoles();
        var requestedIds = request.RoleIds.Distinct().ToArray();
        var roles = await roleRepository.GetByIdsAsync(requestedIds, cancellationToken);
        if (roles.Count != requestedIds.Length || roles.Any(role => !role.IsActive))
            throw InvalidRoles();

        var before = assignment.RoleIds.ToArray();
        assignment.ReplaceRoles(roles);
        await repository.ReplaceUserRolesAsync(assignment, cancellationToken);
        RoleAuthorization.RecordChange(audit, request.ActorUserId, PermissionCodes.AssignUserRole,
            assignment.UserId, before, assignment.RoleIds);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UserRoleDto(assignment.UserId, assignment.Roles, assignment.RoleIds, assignment.ConcurrencyStamp);
    }

    private static RequestValidationException InvalidRoles() =>
        new(ErrorCodes.InvalidRole, "Vai trò không tồn tại hoặc đã bị vô hiệu hóa.",
            new Dictionary<string, string[]> { ["roleIds"] = ["Chỉ được gán vai trò đang hoạt động trong cơ sở dữ liệu."] });
}
