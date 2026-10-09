using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Enums;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Matching;

public sealed record CreateListingRequestCommand(Guid ActorUserId, Guid ListingId, Guid? ApplicantTeamId, string? Message) : IRequest<ListingRequestDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.CreateRequests];
}
public sealed record AcceptListingRequestCommand(Guid ActorUserId, Guid ListingId, Guid RequestId, Guid ConcurrencyToken) : IRequest<ListingRequestDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.AcceptRequests];
}
public sealed record RejectListingRequestCommand(Guid ActorUserId, Guid ListingId, Guid RequestId, Guid ConcurrencyToken, string Reason) : IRequest<ListingRequestDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.RejectRequests];
}
public sealed record WithdrawListingRequestCommand(Guid ActorUserId, Guid ListingId, Guid RequestId, Guid ConcurrencyToken) : IRequest<ListingRequestDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.WithdrawRequests];
}
public sealed class ListingRequestHandlers(IMatchingRepository repository, MatchingServices services, MatchingNotifications notifications,
    ITeamRepository teams, IMapper mapper, TimeProvider clock) : IRequestHandler<CreateListingRequestCommand, ListingRequestDto>,
    IRequestHandler<AcceptListingRequestCommand, ListingRequestDto>, IRequestHandler<RejectListingRequestCommand, ListingRequestDto>,
    IRequestHandler<WithdrawListingRequestCommand, ListingRequestDto>
{
    public async Task<ListingRequestDto> Handle(CreateListingRequestCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var listing = await services.ListingAsync(request.ListingId, true, cancellationToken);
        await services.RequireActiveUserAsync(request.ActorUserId, cancellationToken);
        await services.RequireFormatAsync(listing.SportId, listing.FormatId, cancellationToken);
        if (listing.HostTeamId is { } hostId)
        {
            var host = await teams.GetByIdAsync(hostId, cancellationToken);
            if (host is null || host.Status != TeamStatus.ACTIVE)
                throw new ConflictException(ErrorCodes.InvalidMatchingOperation, "Đội chủ nhà không hoạt động.");
            if (host.IsCaptain(request.ActorUserId) || host.IsMember(request.ActorUserId))
                throw new ConflictException(ErrorCodes.InvalidMatchingOperation, "Thành viên đội chủ nhà không được đăng ký kèo của đội.");
        }
        if (request.ApplicantTeamId is { } teamId)
            _ = await services.RequireTeamManagerAsync(teamId, request.ActorUserId, listing.SportId, cancellationToken);
        var application = listing.RequestToJoin(request.ApplicantTeamId is null ? request.ActorUserId : null,
            request.ApplicantTeamId, request.Message, clock.GetUtcNow());
        await notifications.PublishAsync(listing, request.ActorUserId, NotificationType.REQUEST_CREATED,
            await notifications.HostManagersAsync(listing, cancellationToken), cancellationToken, application.Id);
        await services.SaveAsync(listing, transaction, cancellationToken);
        return mapper.Map<ListingRequestDto>(application);
    }
    public async Task<ListingRequestDto> Handle(AcceptListingRequestCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var listing = await services.ListingAsync(request.ListingId, true, cancellationToken);
        await services.RequireHostAsync(listing, request.ActorUserId, cancellationToken);
        if (listing.HostTeamId is { } hostId)
            _ = await services.RequireTeamManagerAsync(hostId, request.ActorUserId, listing.SportId, cancellationToken);
        await services.RequireFormatAsync(listing.SportId, listing.FormatId, cancellationToken);
        var application = MatchingServices.Request(listing, request.RequestId);
        MatchingServices.RequireVersion(application, request.ConcurrencyToken);
        await services.RequireActiveApplicantAsync(listing, application, cancellationToken);
        listing.AcceptRequest(application, request.ActorUserId, clock.GetUtcNow());
        await notifications.PublishAsync(listing, request.ActorUserId, NotificationType.REQUEST_ACCEPTED,
            await notifications.ApplicantsAsync([application], cancellationToken), cancellationToken, application.Id);
        await services.SaveAsync(listing, transaction, cancellationToken);
        return mapper.Map<ListingRequestDto>(application);
    }
    public async Task<ListingRequestDto> Handle(RejectListingRequestCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var listing = await services.ListingAsync(request.ListingId, true, cancellationToken);
        await services.RequireHostAsync(listing, request.ActorUserId, cancellationToken);
        var application = MatchingServices.Request(listing, request.RequestId);
        MatchingServices.RequireVersion(application, request.ConcurrencyToken);
        listing.RejectRequest(application, request.ActorUserId, request.Reason, clock.GetUtcNow());
        await notifications.PublishAsync(listing, request.ActorUserId, NotificationType.REQUEST_REJECTED,
            await notifications.ApplicantsAsync([application], cancellationToken), cancellationToken, application.Id);
        await services.SaveAsync(listing, transaction, cancellationToken);
        return mapper.Map<ListingRequestDto>(application);
    }
    public async Task<ListingRequestDto> Handle(WithdrawListingRequestCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var listing = await services.ListingAsync(request.ListingId, true, cancellationToken);
        var application = MatchingServices.Request(listing, request.RequestId);
        await services.RequireApplicantAsync(listing, application, request.ActorUserId, cancellationToken);
        MatchingServices.RequireVersion(application, request.ConcurrencyToken);
        listing.WithdrawRequest(application, clock.GetUtcNow());
        await notifications.PublishAsync(listing, request.ActorUserId, NotificationType.REQUEST_WITHDRAWN,
            await notifications.HostManagersAsync(listing, cancellationToken), cancellationToken, application.Id);
        repository.AddEvent(MatchingEvents.Create(MatchingEvents.ListingChanged, listing.Id, clock.GetUtcNow()));
        await services.SaveAsync(listing, transaction, cancellationToken);
        return mapper.Map<ListingRequestDto>(application);
    }
}
