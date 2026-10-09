using MatchingSports.Api.Contracts;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Features.PublicProfile;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MatchingSports.Api.Controllers;

[Authorize]
[Route("api/v1")]
public sealed class PublicProfilesController(IMediator mediator, ICurrentUser currentUser) : ApiControllerBase
{
    [HttpGet("users/{userId:guid}/public-profile"), Authorize(Policy = PermissionCodes.ReadPublicUserProfiles)]
    [ProducesResponseType<BaseResponse<PublicUserProfileDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<PublicUserProfileDto>>> GetUserProfile(Guid userId, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new GetPublicUserProfileQuery(currentUser.UserId, userId), cancellationToken));

    [HttpGet("teams/{teamId:guid}/public-profile"), Authorize(Policy = PermissionCodes.ReadPublicTeamProfiles)]
    [ProducesResponseType<BaseResponse<PublicTeamProfileDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<PublicTeamProfileDto>>> GetTeamProfile(Guid teamId, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new GetPublicTeamProfileQuery(currentUser.UserId, teamId), cancellationToken));
}
