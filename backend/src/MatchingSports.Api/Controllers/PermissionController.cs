using AutoMapper;
using MatchingSports.Api.Contracts;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Permission;
using MatchingSports.Application.Features.Role;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MatchingSports.Api.Controllers;

[Route("api/v1/permissions")]
[Authorize]
public sealed class PermissionController(IMediator mediator, ICurrentUser currentUser, IMapper mapper) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCodes.ListPermissions)]
    [ProducesResponseType<PaginatedResponse<PermissionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<PermissionDto>>> GetPermissions([FromQuery] GetPermissionsRequest request, CancellationToken cancellationToken)
    {
        var query = mapper.Map<GetPermissionsQuery>(request) with { ActorUserId = currentUser.UserId };
        var result = await mediator.Send(query, cancellationToken);
        return OkPaginatedResponse(result, item => item);
    }

    [HttpGet("{permissionId:guid}")]
    [Authorize(Policy = PermissionCodes.ReadPermissions)]
    [ProducesResponseType<BaseResponse<PermissionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<PermissionDto>>> GetPermission(Guid permissionId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPermissionQuery(currentUser.UserId, permissionId), cancellationToken);
        return OkResponse(result);
    }

}
