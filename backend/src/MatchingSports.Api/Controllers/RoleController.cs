using AutoMapper;
using MatchingSports.Api.Contracts;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Role;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MatchingSports.Api.Controllers;

[Route("api/v1/roles")]
[Authorize]
public sealed class RoleController(IMediator mediator, ICurrentUser currentUser, IMapper mapper) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCodes.ListRoles)]
    [ProducesResponseType<PaginatedResponse<RoleDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<RoleDto>>> GetRoles([FromQuery] GetRolesRequest request, CancellationToken cancellationToken)
    {
        var query = mapper.Map<GetRolesQuery>(request) with { ActorUserId = currentUser.UserId };
        var result = await mediator.Send(query, cancellationToken);
        return OkPaginatedResponse(result, item => item);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.CreateRoles)]
    [ProducesResponseType<BaseResponse<RoleDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<RoleDto>>> CreateRole(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateRoleCommand(currentUser.UserId, request.Name,
            request.Description, request.IsDefaultForRegistration), cancellationToken);
        return OkResponse(result);
    }

    [HttpPut("{roleId:guid}")]
    [Authorize(Policy = PermissionCodes.UpdateRoles)]
    [ProducesResponseType<BaseResponse<RoleDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<RoleDto>>> UpdateRole(Guid roleId, UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateRoleCommand(currentUser.UserId, roleId, request.Name,
            request.Description, request.IsActive!.Value, request.IsDefaultForRegistration!.Value,
            request.ConcurrencyStamp), cancellationToken);
        return OkResponse(result);
    }

    [HttpGet("matrix")]
    [Authorize(Policy = PermissionCodes.ListRoles)]
    [Authorize(Policy = PermissionCodes.ListPermissions)]
    [ProducesResponseType<BaseResponse<RolePermissionMatrixDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<RolePermissionMatrixDto>>> GetRolePermissionMatrix([FromQuery] GetRolePermissionMatrixRequest request, CancellationToken cancellationToken)
    {
        var query = mapper.Map<GetRolePermissionMatrixQuery>(request) with { ActorUserId = currentUser.UserId };
        var result = await mediator.Send(query, cancellationToken);
        return OkResponse(result);
    }

    [HttpGet("users/{userId:guid}")]
    [Authorize(Policy = PermissionCodes.ReadUsers)]
    [ProducesResponseType<BaseResponse<UserRoleDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<UserRoleDto>>> GetUserRoles(Guid userId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUserRolesQuery(currentUser.UserId, userId), cancellationToken);
        return OkResponse(result);
    }

    [HttpPut("users/{userId:guid}")]
    [Authorize(Policy = PermissionCodes.AssignUserRole)]
    [ProducesResponseType<BaseResponse<UserRoleDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseResponse<UserRoleDto>>> ReplaceUserRoles(
        Guid userId,
        [FromBody] ReplaceUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ReplaceUserRolesCommand(currentUser.UserId, userId, request.RoleIds, request.ConcurrencyStamp), cancellationToken);
        return OkResponse(result);
    }

    [HttpPut("{roleId:guid}/permissions")]
    [Authorize(Policy = PermissionCodes.UpdateRolePermissions)]
    [ProducesResponseType<BaseResponse<RoleDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<RoleDto>>> UpdateRolePermissions(
        Guid roleId,
        [FromBody] UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateRolePermissionsCommand(currentUser.UserId, roleId, request.PermissionCodes, request.ConcurrencyStamp), cancellationToken);
        return OkResponse(result);
    }

    [HttpGet("{roleId:guid}")]
    [Authorize(Policy = PermissionCodes.ReadRoles)]
    [ProducesResponseType<BaseResponse<RoleDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<RoleDto>>> GetRole(Guid roleId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRoleQuery(currentUser.UserId, roleId), cancellationToken);
        return OkResponse(result);
    }

}
