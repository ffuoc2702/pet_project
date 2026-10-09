using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Entities;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Matching;

public sealed record CreateListingWatchCommand(Guid ActorUserId, ListingWatchInput Criteria, bool EmailEnabled) : IRequest<ListingWatchDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.CreateListingWatches];
}
public sealed record UpdateListingWatchCommand(Guid ActorUserId, Guid WatchId, Guid ConcurrencyToken, ListingWatchInput Criteria, bool EmailEnabled) : IRequest<ListingWatchDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.UpdateListingWatches];
}
public sealed record PauseListingWatchCommand(Guid ActorUserId, Guid WatchId, Guid ConcurrencyToken) : IRequest<ListingWatchDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.PauseListingWatches];
}
public sealed record ActivateListingWatchCommand(Guid ActorUserId, Guid WatchId, Guid ConcurrencyToken) : IRequest<ListingWatchDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ActivateListingWatches];
}
public sealed record CancelListingWatchCommand(Guid ActorUserId, Guid WatchId, Guid ConcurrencyToken) : IRequest<ListingWatchDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.CancelListingWatches];
}
public sealed class ListingWatchHandlers(IMatchingRepository repository, MatchingServices services, MatchingNotifications notifications,
    IMapper mapper, TimeProvider clock) : IRequestHandler<CreateListingWatchCommand, ListingWatchDto>,
    IRequestHandler<UpdateListingWatchCommand, ListingWatchDto>, IRequestHandler<PauseListingWatchCommand, ListingWatchDto>,
    IRequestHandler<ActivateListingWatchCommand, ListingWatchDto>, IRequestHandler<CancelListingWatchCommand, ListingWatchDto>
{
    public async Task<ListingWatchDto> Handle(CreateListingWatchCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var settings = await services.SettingsAsync(cancellationToken);
        await services.RequireActiveUserAsync(request.ActorUserId, cancellationToken);
        await services.RequireFormatAsync(request.Criteria.SportId, request.Criteria.FormatId, cancellationToken);
        notifications.RequireEmail(request.EmailEnabled);
        MatchingServices.EnsureWatchCapacity(settings, await repository.CountActiveWatchesAsync(request.ActorUserId, clock.GetUtcNow(), cancellationToken));
        var watch = ListingWatch.Create(request.ActorUserId, request.Criteria.ToCriteria(), request.EmailEnabled, settings, clock.GetUtcNow());
        repository.Add(watch);
        repository.AddEvent(MatchingEvents.Create(MatchingEvents.WatchChanged, watch.Id, clock.GetUtcNow()));
        await services.SaveAsync(null, transaction, cancellationToken);
        return mapper.Map<ListingWatchDto>(watch);
    }
    public async Task<ListingWatchDto> Handle(UpdateListingWatchCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var watch = await OwnedWatchAsync(request.ActorUserId, request.WatchId, request.ConcurrencyToken, cancellationToken);
        var settings = await services.SettingsAsync(cancellationToken);
        await services.RequireFormatAsync(request.Criteria.SportId, request.Criteria.FormatId, cancellationToken);
        notifications.RequireEmail(request.EmailEnabled);
        watch.Expire(clock.GetUtcNow());
        watch.UpdateCriteria(request.Criteria.ToCriteria(), request.EmailEnabled, settings, clock.GetUtcNow());
        repository.AddEvent(MatchingEvents.Create(MatchingEvents.WatchChanged, watch.Id, clock.GetUtcNow()));
        await services.SaveAsync(null, transaction, cancellationToken);
        return mapper.Map<ListingWatchDto>(watch);
    }
    public async Task<ListingWatchDto> Handle(PauseListingWatchCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var watch = await OwnedWatchAsync(request.ActorUserId, request.WatchId, request.ConcurrencyToken, cancellationToken);
        watch.Pause(clock.GetUtcNow());
        await services.SaveAsync(null, transaction, cancellationToken);
        return mapper.Map<ListingWatchDto>(watch);
    }
    public async Task<ListingWatchDto> Handle(ActivateListingWatchCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var watch = await OwnedWatchAsync(request.ActorUserId, request.WatchId, request.ConcurrencyToken, cancellationToken);
        var settings = await services.SettingsAsync(cancellationToken);
        MatchingServices.EnsureWatchCapacity(settings, await repository.CountActiveWatchesAsync(request.ActorUserId, clock.GetUtcNow(), cancellationToken));
        await services.RequireFormatAsync(watch.SportId, watch.FormatId, cancellationToken);
        notifications.RequireEmail(watch.EmailEnabled);
        watch.Activate(settings, clock.GetUtcNow());
        repository.AddEvent(MatchingEvents.Create(MatchingEvents.WatchChanged, watch.Id, clock.GetUtcNow()));
        await services.SaveAsync(null, transaction, cancellationToken);
        return mapper.Map<ListingWatchDto>(watch);
    }
    public async Task<ListingWatchDto> Handle(CancelListingWatchCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var watch = await OwnedWatchAsync(request.ActorUserId, request.WatchId, request.ConcurrencyToken, cancellationToken);
        watch.Cancel();
        await services.SaveAsync(null, transaction, cancellationToken);
        return mapper.Map<ListingWatchDto>(watch);
    }
    private async Task<ListingWatch> OwnedWatchAsync(Guid actorId, Guid id, Guid token, CancellationToken cancellationToken)
    {
        var watch = await services.WatchAsync(id, true, cancellationToken);
        MatchingServices.RequireOwner(watch.UserId, actorId);
        MatchingServices.RequireVersion(watch, token);
        return watch;
    }
}
