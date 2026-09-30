using System.Security.Claims;
using AutoMapper;
using MatchingSports.Api.Contracts;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.UserProfile;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MatchingSports.Api.Controllers;

[Route("api/v1/user-profile")]
[Authorize]
public sealed class UserProfileController(IMediator mediator, IMapper mapper) : ApiControllerBase
{
    [HttpPut]
    [ProducesResponseType<BaseResponse<UserProfileDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseResponse<UserProfileDto>>> UpdateUserProfile(
        [FromBody] UpdateUserProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            throw new UnauthorizedException(ErrorCodes.InvalidAccessToken, "Access token không hợp lệ.");

        var result = await mediator.Send(new UpdateUserProfileCommand(
            userId,
            request.DisplayName,
            request.AvatarUrl,
            request.DefaultArea,
            request.ContactNote), cancellationToken);

        return OkResponse(
            mapper.Map<UserProfileDto>(result),
            "Cập nhật hồ sơ thành công.");
    }
}
