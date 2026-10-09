using AutoMapper;
using MatchingSports.Api.Contracts;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Features.Matching;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MatchingSports.Api.Controllers;

[Route("api/v1/listings")]
[Authorize]
public sealed class ListingsController(IMediator mediator, ICurrentUser currentUser, IMapper mapper) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCodes.ListListings)]
    [ProducesResponseType<PaginatedResponse<ListingDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<ListingDto>>> GetListings([FromQuery] GetListingsRequest request, CancellationToken cancellationToken)
    {
        var query = mapper.Map<GetListingsQuery>(request) with { ActorUserId = currentUser.UserId };
        return OkPaginatedResponse(await mediator.Send(query, cancellationToken), item => item);
    }
    [HttpGet("{listingId:guid}")]
    [Authorize(Policy = PermissionCodes.ReadListings)]
    [ProducesResponseType<BaseResponse<ListingDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<ListingDto>>> GetListing(Guid listingId, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new GetListingQuery(currentUser.UserId, listingId), cancellationToken));
    [HttpPost]
    [Authorize(Policy = PermissionCodes.CreateListings)]
    [ProducesResponseType<BaseResponse<ListingDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<ListingDto>>> CreateListing([FromBody] CreateListingRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new CreateListingCommand(currentUser.UserId, mapper.Map<ListingInput>(request)), cancellationToken));
    [HttpPut("{listingId:guid}")]
    [Authorize(Policy = PermissionCodes.UpdateListings)]
    [ProducesResponseType<BaseResponse<ListingDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<ListingDto>>> UpdateListing(Guid listingId, [FromBody] UpdateListingRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new UpdateListingCommand(currentUser.UserId, listingId,
            request.ConcurrencyToken!.Value, mapper.Map<ListingInput>(request)), cancellationToken));
    [HttpPost("{listingId:guid}/cancel")]
    [Authorize(Policy = PermissionCodes.CancelListings)]
    [ProducesResponseType<BaseResponse<ListingDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<ListingDto>>> CancelListing(Guid listingId, [FromBody] MatchingReasonRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new CancelListingCommand(currentUser.UserId, listingId, request.ConcurrencyToken!.Value, request.Reason), cancellationToken));
}
