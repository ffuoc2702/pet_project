using MatchingSports.Api.Contracts;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Features.UserSportProfile;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MatchingSports.Api.Controllers;

[Route("api/v1/user-sport-profiles")]
[Authorize(Policy = PermissionCodes.UpdateSportProfiles)]
public sealed class UserSportProfileController(IMediator mediator, ICurrentUser currentUser) : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType<BaseResponse<UserSportProfileDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<UserSportProfileDto>>> CreateUserSportProfile(
        [FromBody] CreateUserSportProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateUserSportProfileCommand(currentUser.UserId,
            request.SportId, request.Level, request.PreferredFormatIds, request.Bio), cancellationToken);
        return OkResponse(result, "Thêm hồ sơ thể thao thành công.");
    }

    [HttpPut("{profileId:guid}")]
    [ProducesResponseType<BaseResponse<UserSportProfileDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<UserSportProfileDto>>> UpdateUserSportProfile(Guid profileId,
        [FromBody] UpdateUserSportProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateUserSportProfileCommand(currentUser.UserId,
            profileId, request.Level, request.PreferredFormatIds, request.Bio), cancellationToken);
        return OkResponse(result, "Cập nhật hồ sơ thể thao thành công.");
    }

    [HttpDelete("{profileId:guid}")]
    [ProducesResponseType<BaseResponse<object>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<object>>> DeleteUserSportProfile(Guid profileId, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteUserSportProfileCommand(currentUser.UserId, profileId), cancellationToken);
        return OkResponse<object>(new { }, "Xoá hồ sơ thể thao thành công.");
    }
}
