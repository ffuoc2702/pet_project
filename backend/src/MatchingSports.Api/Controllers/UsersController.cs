using AutoMapper;
using MatchingSports.Api.Contracts;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Features.User;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MatchingSports.Api.Controllers;

[Route("api/v1/users")]
[Authorize]
public sealed class UsersController(IMediator mediator, ICurrentUser currentUser, IMapper mapper) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCodes.ListUsers)]
    [ProducesResponseType<PaginatedResponse<UserListDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<UserListDto>>> GetUsers([FromQuery] GetUsersRequest request, CancellationToken cancellationToken)
    {
        var query = mapper.Map<GetUsersQuery>(request) with { ActorUserId = currentUser.UserId };
        return OkPaginatedResponse(await mediator.Send(query, cancellationToken), item => item);
    }
}
