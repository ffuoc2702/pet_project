using AutoMapper;
using MatchingSports.Api.Contracts;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Features.Matching;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MatchingSports.Api.Controllers;

[Route("api/v1/listing-watches")]
[Authorize]
public sealed class ListingWatchesController(IMediator mediator, ICurrentUser currentUser, IMapper mapper) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCodes.ListListingWatches)]
    [ProducesResponseType<PaginatedResponse<ListingWatchDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<ListingWatchDto>>> GetListingWatches([FromQuery] GetListingWatchesRequest request, CancellationToken cancellationToken)
    {
        var query = mapper.Map<GetListingWatchesQuery>(request) with { ActorUserId = currentUser.UserId };
        return OkPaginatedResponse(await mediator.Send(query, cancellationToken), item => item);
    }
    [HttpGet("{watchId:guid}")]
    [Authorize(Policy = PermissionCodes.ReadListingWatches)]
    [ProducesResponseType<BaseResponse<ListingWatchDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<ListingWatchDto>>> GetListingWatch(Guid watchId, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new GetListingWatchQuery(currentUser.UserId, watchId), cancellationToken));
    [HttpPost]
    [Authorize(Policy = PermissionCodes.CreateListingWatches)]
    [ProducesResponseType<BaseResponse<ListingWatchDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<ListingWatchDto>>> CreateListingWatch([FromBody] ListingWatchRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new CreateListingWatchCommand(currentUser.UserId, mapper.Map<ListingWatchInput>(request), request.EmailEnabled), cancellationToken));
    [HttpPut("{watchId:guid}")]
    [Authorize(Policy = PermissionCodes.UpdateListingWatches)]
    [ProducesResponseType<BaseResponse<ListingWatchDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<ListingWatchDto>>> UpdateListingWatch(Guid watchId, [FromBody] UpdateListingWatchRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new UpdateListingWatchCommand(currentUser.UserId, watchId,
            request.ConcurrencyToken!.Value, mapper.Map<ListingWatchInput>(request), request.EmailEnabled), cancellationToken));
    [HttpPost("{watchId:guid}/pause")]
    [Authorize(Policy = PermissionCodes.PauseListingWatches)]
    [ProducesResponseType<BaseResponse<ListingWatchDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<ListingWatchDto>>> PauseListingWatch(Guid watchId, [FromBody] MatchingVersionRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new PauseListingWatchCommand(currentUser.UserId, watchId, request.ConcurrencyToken!.Value), cancellationToken));
    [HttpPost("{watchId:guid}/activate")]
    [Authorize(Policy = PermissionCodes.ActivateListingWatches)]
    [ProducesResponseType<BaseResponse<ListingWatchDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<ListingWatchDto>>> ActivateListingWatch(Guid watchId, [FromBody] MatchingVersionRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new ActivateListingWatchCommand(currentUser.UserId, watchId, request.ConcurrencyToken!.Value), cancellationToken));
    [HttpPost("{watchId:guid}/cancel")]
    [Authorize(Policy = PermissionCodes.CancelListingWatches)]
    [ProducesResponseType<BaseResponse<ListingWatchDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<ListingWatchDto>>> CancelListingWatch(Guid watchId, [FromBody] MatchingVersionRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new CancelListingWatchCommand(currentUser.UserId, watchId, request.ConcurrencyToken!.Value), cancellationToken));
}
