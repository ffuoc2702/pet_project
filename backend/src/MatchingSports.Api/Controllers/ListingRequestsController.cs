using AutoMapper;
using MatchingSports.Api.Contracts;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Features.Matching;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MatchingSports.Api.Controllers;

[Route("api/v1/listings/{listingId:guid}/requests")]
[Authorize]
public sealed class ListingRequestsController(IMediator mediator, ICurrentUser currentUser, IMapper mapper) : ApiControllerBase
{
    [HttpGet("/api/v1/listing-requests")]
    [Authorize(Policy = PermissionCodes.ListRequests)]
    [ProducesResponseType<PaginatedResponse<ListingRequestDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<ListingRequestDto>>> GetOwnRequests([FromQuery] GetListingRequestsRequest request, CancellationToken cancellationToken)
    {
        var query = mapper.Map<GetListingRequestsQuery>(request) with { ActorUserId = currentUser.UserId };
        return OkPaginatedResponse(await mediator.Send(query, cancellationToken), item => item);
    }
    [HttpGet]
    [Authorize(Policy = PermissionCodes.ListRequests)]
    [ProducesResponseType<PaginatedResponse<ListingRequestDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<ListingRequestDto>>> GetListingRequests(Guid listingId, [FromQuery] GetListingRequestsRequest request, CancellationToken cancellationToken)
    {
        var query = mapper.Map<GetListingRequestsQuery>(request) with { ActorUserId = currentUser.UserId, ListingId = listingId };
        return OkPaginatedResponse(await mediator.Send(query, cancellationToken), item => item);
    }
    [HttpGet("{requestId:guid}")]
    [Authorize(Policy = PermissionCodes.ReadRequests)]
    [ProducesResponseType<BaseResponse<ListingRequestDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<ListingRequestDto>>> GetListingRequest(Guid listingId, Guid requestId, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new GetListingRequestQuery(currentUser.UserId, listingId, requestId), cancellationToken));
    [HttpPost]
    [Authorize(Policy = PermissionCodes.CreateRequests)]
    [ProducesResponseType<BaseResponse<ListingRequestDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<ListingRequestDto>>> JoinListing(Guid listingId, [FromBody] JoinListingRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new CreateListingRequestCommand(currentUser.UserId, listingId, request.ApplicantTeamId, request.Message), cancellationToken));
    [HttpPost("{requestId:guid}/accept")]
    [Authorize(Policy = PermissionCodes.AcceptRequests)]
    [ProducesResponseType<BaseResponse<ListingRequestDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<ListingRequestDto>>> AcceptRequest(Guid listingId, Guid requestId, [FromBody] MatchingVersionRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new AcceptListingRequestCommand(currentUser.UserId, listingId, requestId, request.ConcurrencyToken!.Value), cancellationToken));
    [HttpPost("{requestId:guid}/reject")]
    [Authorize(Policy = PermissionCodes.RejectRequests)]
    [ProducesResponseType<BaseResponse<ListingRequestDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<ListingRequestDto>>> RejectRequest(Guid listingId, Guid requestId, [FromBody] MatchingReasonRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new RejectListingRequestCommand(currentUser.UserId, listingId, requestId, request.ConcurrencyToken!.Value, request.Reason), cancellationToken));
    [HttpPost("{requestId:guid}/withdraw")]
    [Authorize(Policy = PermissionCodes.WithdrawRequests)]
    [ProducesResponseType<BaseResponse<ListingRequestDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<ListingRequestDto>>> WithdrawRequest(Guid listingId, Guid requestId, [FromBody] MatchingVersionRequest request, CancellationToken cancellationToken) =>
        OkResponse(await mediator.Send(new WithdrawListingRequestCommand(currentUser.UserId, listingId, requestId, request.ConcurrencyToken!.Value), cancellationToken));
}
