using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Matching;

public sealed record CreateListingCommand(Guid ActorUserId, ListingInput Values) : IRequest<ListingDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.CreateListings];
}
public sealed record UpdateListingCommand(Guid ActorUserId, Guid ListingId, Guid ConcurrencyToken, ListingInput Values) : IRequest<ListingDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.UpdateListings];
}
public sealed record CancelListingCommand(Guid ActorUserId, Guid ListingId, Guid ConcurrencyToken, string Reason) : IRequest<ListingDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.CancelListings];
}
public sealed class ListingCommandHandlers(IMatchingRepository repository, MatchingServices services,
    MatchingNotifications notifications, IMapper mapper, TimeProvider clock) :
    IRequestHandler<CreateListingCommand, ListingDto>, IRequestHandler<UpdateListingCommand, ListingDto>, IRequestHandler<CancelListingCommand, ListingDto>
{
    public async Task<ListingDto> Handle(CreateListingCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        await services.RequireActiveUserAsync(request.ActorUserId, cancellationToken);
        await services.RequireFormatAsync(request.Values.SportId, request.Values.FormatId, cancellationToken);
        if (request.Values.HostTeamId is { } teamId)
            _ = await services.RequireTeamManagerAsync(teamId, request.ActorUserId, request.Values.SportId, cancellationToken);
        var listing = Listing.Create(request.ActorUserId, request.Values.ToDomainValues(), clock.GetUtcNow());
        repository.Add(listing);
        repository.AddEvent(MatchingEvents.Create(MatchingEvents.ListingChanged, listing.Id, clock.GetUtcNow()));
        await services.SaveAsync(listing, transaction, cancellationToken);
        return mapper.Map<ListingDto>(listing);
    }
    public async Task<ListingDto> Handle(UpdateListingCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var listing = await services.ListingAsync(request.ListingId, true, cancellationToken);
        await services.RequireHostAsync(listing, request.ActorUserId, cancellationToken);
        MatchingServices.RequireVersion(listing, request.ConcurrencyToken);
        await services.RequireFormatAsync(request.Values.SportId, request.Values.FormatId, cancellationToken);
        listing.UpdateDetails(request.Values.ToDomainValues(), clock.GetUtcNow());
        await notifications.PublishAsync(listing, request.ActorUserId, NotificationType.LISTING_UPDATED,
            await notifications.ApplicantsAsync(listing.Requests.Where(item => item.Status == RequestStatus.PENDING), cancellationToken), cancellationToken);
        repository.AddEvent(MatchingEvents.Create(MatchingEvents.ListingChanged, listing.Id, clock.GetUtcNow()));
        await services.SaveAsync(listing, transaction, cancellationToken);
        return mapper.Map<ListingDto>(listing);
    }
    public async Task<ListingDto> Handle(CancelListingCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var listing = await services.ListingAsync(request.ListingId, true, cancellationToken);
        await services.RequireHostAsync(listing, request.ActorUserId, cancellationToken, captainOnly: true);
        if (listing.Match is not null) throw new ConflictException(ErrorCodes.InvalidMatchingOperation, "Kèo đã có trận; hãy dùng thao tác hủy trận.");
        if (listing.Status == ListingStatus.CANCELLED) return mapper.Map<ListingDto>(listing);
        MatchingServices.RequireVersion(listing, request.ConcurrencyToken);
        var recipients = (await notifications.ApplicantsAsync(listing.Requests.Where(item => item.Status == RequestStatus.PENDING), cancellationToken))
            .Concat(await notifications.HostManagersAsync(listing, cancellationToken));
        listing.Cancel(request.ActorUserId, request.Reason, clock.GetUtcNow());
        await notifications.PublishAsync(listing, request.ActorUserId, NotificationType.LISTING_CANCELLED, recipients, cancellationToken, includeActor: true);
        await services.SaveAsync(listing, transaction, cancellationToken);
        return mapper.Map<ListingDto>(listing);
    }
}
