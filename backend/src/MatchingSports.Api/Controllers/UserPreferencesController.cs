using MatchingSports.Api.Contracts;
using MatchingSports.Application.Features.UserPreferences;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MatchingSports.Api.Controllers;

[Authorize]
[Route("api/v1/me/preferences")]
public sealed class UserPreferencesController(IMediator mediator) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<BaseResponse<UserPreferencesDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<UserPreferencesDto?>>> Get(CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new GetUserPreferencesQuery(), cancellationToken));

    [HttpPut]
    [ProducesResponseType<BaseResponse<UserPreferencesDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<UserPreferencesDto>>> Update(
        UpdateUserPreferencesRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new UpdateUserPreferencesCommand(request.LanguageCode, request.ColorMode,
            request.ThemePreset, request.ConcurrencyToken), cancellationToken));
}
