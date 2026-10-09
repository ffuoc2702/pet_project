using System.Text.Json;
using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Matching;

public sealed record ConfirmMatchCommand(Guid ActorUserId, Guid MatchId, Guid ConcurrencyToken) : IRequest<MatchDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ConfirmMatches];
}
public sealed record CancelMatchCommand(Guid ActorUserId, Guid MatchId, Guid ConcurrencyToken, string Reason) : IRequest<MatchDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.CancelMatches];
}
public sealed record CancelAnyMatchCommand(Guid ActorUserId, Guid MatchId, Guid ConcurrencyToken, string Reason) : IRequest<MatchDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.CancelAnyMatch];
}
public sealed record CompleteMatchCommand(Guid ActorUserId, Guid MatchId, Guid ConcurrencyToken) : IRequest<MatchDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.CompleteMatches];
}
public sealed class MatchCommandHandlers(IMatchingRepository repository, MatchingServices services, MatchingNotifications notifications,
    IAdminActionRepository audit, IMapper mapper, TimeProvider clock) : IRequestHandler<ConfirmMatchCommand, MatchDto>,
    IRequestHandler<CancelMatchCommand, MatchDto>, IRequestHandler<CancelAnyMatchCommand, MatchDto>, IRequestHandler<CompleteMatchCommand, MatchDto>
{
    public async Task<MatchDto> Handle(ConfirmMatchCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var listing = await services.MatchListingAsync(request.MatchId, true, cancellationToken);
        await services.RequireHostAsync(listing, request.ActorUserId, cancellationToken);
        if (listing.HostTeamId is { } hostId)
            _ = await services.RequireTeamManagerAsync(hostId, request.ActorUserId, listing.SportId, cancellationToken);
        MatchingServices.RequireVersion(listing.Match!, request.ConcurrencyToken);
        await services.RequireFormatAsync(listing.SportId, listing.FormatId, cancellationToken);
        foreach (var application in listing.Requests.Where(item => item.Status == RequestStatus.ACCEPTED))
            await services.RequireActiveApplicantAsync(listing, application, cancellationToken);
        var pending = listing.Requests.Where(item => item.Status == RequestStatus.PENDING).ToArray();
        listing.ConfirmMatch(clock.GetUtcNow());
        await notifications.PublishAsync(listing, request.ActorUserId, NotificationType.MATCH_CONFIRMED,
            await notifications.ParticipantsAsync(listing, cancellationToken), cancellationToken, includeActor: true);
        await notifications.PublishAsync(listing, request.ActorUserId, NotificationType.REQUEST_EXPIRED,
            await notifications.ApplicantsAsync(pending, cancellationToken), cancellationToken);
        await services.SaveAsync(listing, transaction, cancellationToken);
        return mapper.Map<MatchDto>(listing.Match);
    }
    public Task<MatchDto> Handle(CancelMatchCommand request, CancellationToken cancellationToken) =>
        CancelAsync(request.ActorUserId, request.MatchId, request.ConcurrencyToken, request.Reason, false, cancellationToken);
    public Task<MatchDto> Handle(CancelAnyMatchCommand request, CancellationToken cancellationToken) =>
        CancelAsync(request.ActorUserId, request.MatchId, request.ConcurrencyToken, request.Reason, true, cancellationToken);
    private async Task<MatchDto> CancelAsync(Guid actorId, Guid matchId, Guid token, string reason, bool administrative, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var listing = await services.MatchListingAsync(matchId, true, cancellationToken);
        if (!administrative) await services.RequireHostAsync(listing, actorId, cancellationToken, captainOnly: true);
        if (listing.Match!.Status == MatchStatus.CANCELLED) return mapper.Map<MatchDto>(listing.Match);
        MatchingServices.RequireVersion(listing.Match, token);
        var recipients = (await notifications.ParticipantsAsync(listing, cancellationToken))
            .Concat(await notifications.ApplicantsAsync(listing.Requests.Where(item => item.Status == RequestStatus.PENDING), cancellationToken)).ToArray();
        var before = administrative ? Snapshot(listing.Match) : null;
        listing.Cancel(actorId, reason, clock.GetUtcNow(), administrative);
        if (administrative) audit.Add(AdminAction.Create(actorId, AdminActionType.CANCEL_MATCH,
            EntityTypes.Match, matchId, before!, Snapshot(listing.Match), reason: listing.Match.CancelReason));
        await notifications.PublishAsync(listing, actorId, NotificationType.MATCH_CANCELLED, recipients, cancellationToken, includeActor: true);
        await services.SaveAsync(listing, transaction, cancellationToken);
        return mapper.Map<MatchDto>(listing.Match);
    }
    public async Task<MatchDto> Handle(CompleteMatchCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var listing = await services.MatchListingAsync(request.MatchId, true, cancellationToken);
        await services.RequireHostAsync(listing, request.ActorUserId, cancellationToken);
        MatchingServices.RequireVersion(listing.Match!, request.ConcurrencyToken);
        listing.CompleteMatch(clock.GetUtcNow());
        await notifications.PublishAsync(listing, request.ActorUserId, NotificationType.MATCH_COMPLETED,
            await notifications.ParticipantsAsync(listing, cancellationToken), cancellationToken, includeActor: true);
        await services.SaveAsync(listing, transaction, cancellationToken);
        return mapper.Map<MatchDto>(listing.Match);
    }
    private static string Snapshot(Domain.Entities.Match match) => JsonSerializer.Serialize(new
    { match.Status, match.CancelReason, match.CancelledAt, match.CancelledByUserId });
}
