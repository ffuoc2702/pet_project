using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Enums;
using MatchingSports.Domain.ValueObjects;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Matching;

public sealed record GetListingsQuery : BaseQuery<ListingDto>, IPermissionAuthorizedRequest
{
    public Guid ActorUserId { get; init; }
    public Guid? SportId { get; init; }
    public Guid? FormatId { get; init; }
    public ListingMode? Mode { get; init; }
    public SportLevel? Level { get; init; }
    public DateTimeOffset? StartFrom { get; init; }
    public DateTimeOffset? StartTo { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public int? RadiusMeters { get; init; }
    public bool MineOnly { get; init; }
    public ListingStatus? Status { get; init; }
    public ListingCriteria GetCriteria() => new(SportId, FormatId, Mode, Level, StartFrom, StartTo, Latitude, Longitude, RadiusMeters);
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ListListings];
}
public sealed record GetListingQuery(Guid ActorUserId, Guid ListingId) : IRequest<ListingDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ReadListings];
}
public sealed record GetListingRequestsQuery : BaseQuery<ListingRequestDto>, IPermissionAuthorizedRequest
{
    public Guid ActorUserId { get; init; }
    public Guid? ListingId { get; init; }
    public RequestStatus? Status { get; init; }
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ListRequests];
}
public sealed record GetListingRequestQuery(Guid ActorUserId, Guid ListingId, Guid RequestId) : IRequest<ListingRequestDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ReadRequests];
}
public sealed record GetMatchesQuery : BaseQuery<MatchDto>, IPermissionAuthorizedRequest
{
    public Guid ActorUserId { get; init; }
    public Guid? TeamId { get; init; }
    public MatchStatus? Status { get; init; }
    public DateTimeOffset? StartFrom { get; init; }
    public DateTimeOffset? StartTo { get; init; }
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ListMatches];
}
public sealed record GetMatchQuery(Guid ActorUserId, Guid MatchId) : IRequest<MatchDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ReadMatches];
}
public sealed record MatchContactsDto(Guid MatchId, IReadOnlyList<MatchContactDto> Contacts);
public sealed record GetMatchContactsQuery(Guid ActorUserId, Guid MatchId) : IRequest<MatchContactsDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ReadMatchContact];
}
public sealed record GetListingWatchesQuery : BaseQuery<ListingWatchDto>, IPermissionAuthorizedRequest
{
    public Guid ActorUserId { get; init; }
    public Guid? SportId { get; init; }
    public ListingWatchStatus? Status { get; init; }
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ListListingWatches];
}
public sealed record GetListingWatchQuery(Guid ActorUserId, Guid WatchId) : IRequest<ListingWatchDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ReadListingWatches];
}

public sealed class MatchingQueryHandlers(IMatchingRepository repository, MatchingServices services, ITeamRepository teams,
    IMapper mapper, TimeProvider clock) : IRequestHandler<GetListingsQuery, PaginatedResult<ListingDto>>,
    IRequestHandler<GetListingQuery, ListingDto>, IRequestHandler<GetListingRequestsQuery, PaginatedResult<ListingRequestDto>>,
    IRequestHandler<GetListingRequestQuery, ListingRequestDto>, IRequestHandler<GetMatchesQuery, PaginatedResult<MatchDto>>,
    IRequestHandler<GetMatchQuery, MatchDto>, IRequestHandler<GetMatchContactsQuery, MatchContactsDto>,
    IRequestHandler<GetListingWatchesQuery, PaginatedResult<ListingWatchDto>>, IRequestHandler<GetListingWatchQuery, ListingWatchDto>
{
    public async Task<PaginatedResult<ListingDto>> Handle(GetListingsQuery request, CancellationToken cancellationToken)
    {
        MatchingServices.ValidateCriteria(request.GetCriteria(), (await services.SettingsAsync(cancellationToken)).MaxListingSearchRadiusMeters);
        RequireEnum(request.Status);
        var page = await repository.SearchAsync(request, clock.GetUtcNow(), cancellationToken);
        return page.Map(item => mapper.Map<ListingDto>(item.Listing) with { DistanceMeters = item.DistanceMeters });
    }
    public async Task<ListingDto> Handle(GetListingQuery request, CancellationToken cancellationToken)
    {
        var listing = await services.ListingAsync(request.ListingId, false, cancellationToken);
        if (listing.Status is not (ListingStatus.OPEN or ListingStatus.MATCHED)
            && !await repository.CanViewListingAsync(listing.Id, request.ActorUserId, cancellationToken))
            throw new ForbiddenException(ErrorCodes.Forbidden, "Bạn không có quyền xem kèo này.");
        return mapper.Map<ListingDto>(listing);
    }
    public async Task<PaginatedResult<ListingRequestDto>> Handle(GetListingRequestsQuery request, CancellationToken cancellationToken)
    {
        RequireEnum(request.Status);
        if (request.ListingId is { } id)
            await services.RequireHostAsync(await services.ListingAsync(id, false, cancellationToken), request.ActorUserId, cancellationToken);
        return (await repository.ListRequestsAsync(request, cancellationToken)).Map(mapper.Map<ListingRequestDto>);
    }
    public async Task<ListingRequestDto> Handle(GetListingRequestQuery request, CancellationToken cancellationToken)
    {
        var listing = await services.ListingAsync(request.ListingId, false, cancellationToken);
        var application = MatchingServices.Request(listing, request.RequestId);
        var applicant = application.ApplicantUserId == request.ActorUserId;
        if (application.ApplicantTeamId is { } teamId)
            applicant = (await teams.GetByIdAsync(teamId, cancellationToken))?.CanManageMembers(request.ActorUserId) == true;
        if (!applicant) await services.RequireHostAsync(listing, request.ActorUserId, cancellationToken);
        return mapper.Map<ListingRequestDto>(application);
    }
    public async Task<PaginatedResult<MatchDto>> Handle(GetMatchesQuery request, CancellationToken cancellationToken)
    {
        MatchingServices.ValidateCriteria(new(StartFrom: request.StartFrom, StartTo: request.StartTo), int.MaxValue);
        RequireEnum(request.Status);
        if (request.TeamId is { } teamId)
        {
            var team = await teams.GetByIdAsync(teamId, cancellationToken);
            if (team is null || (!team.IsCaptain(request.ActorUserId) && !team.IsMember(request.ActorUserId)))
                throw new ForbiddenException(ErrorCodes.Forbidden, "Chỉ thành viên được xem lịch trận của đội.");
        }
        return (await repository.ListMatchesAsync(request, cancellationToken)).Map(mapper.Map<MatchDto>);
    }
    public async Task<MatchDto> Handle(GetMatchQuery request, CancellationToken cancellationToken)
    {
        var listing = await services.MatchListingAsync(request.MatchId, false, cancellationToken);
        if (!await repository.CanViewMatchAsync(request.MatchId, request.ActorUserId, false, cancellationToken))
            throw new ForbiddenException(ErrorCodes.Forbidden, "Bạn không tham gia trận này.");
        return mapper.Map<MatchDto>(listing.Match);
    }
    public async Task<MatchContactsDto> Handle(GetMatchContactsQuery request, CancellationToken cancellationToken)
    {
        var listing = await services.MatchListingAsync(request.MatchId, false, cancellationToken);
        if (listing.Match!.Status != MatchStatus.CONFIRMED
            || !await repository.CanViewMatchAsync(request.MatchId, request.ActorUserId, true, cancellationToken))
            throw new ForbiddenException(ErrorCodes.Forbidden, "Liên hệ chỉ dành cho người tham gia trận đã xác nhận.");
        return new(request.MatchId, await repository.GetContactsAsync(request.MatchId, cancellationToken));
    }
    public async Task<PaginatedResult<ListingWatchDto>> Handle(GetListingWatchesQuery request, CancellationToken cancellationToken)
    {
        RequireEnum(request.Status);
        return (await repository.ListWatchesAsync(request, clock.GetUtcNow(), cancellationToken)).Map(watch =>
            mapper.Map<ListingWatchDto>(watch) with { Status = watch.EffectiveStatus(clock.GetUtcNow()) });
    }
    public async Task<ListingWatchDto> Handle(GetListingWatchQuery request, CancellationToken cancellationToken)
    {
        var watch = await services.WatchAsync(request.WatchId, false, cancellationToken);
        MatchingServices.RequireOwner(watch.UserId, request.ActorUserId);
        return mapper.Map<ListingWatchDto>(watch) with { Status = watch.EffectiveStatus(clock.GetUtcNow()) };
    }
    private static void RequireEnum<T>(T? value) where T : struct, Enum
    {
        if (value is not null && !Enum.IsDefined(value.Value))
            throw new RequestValidationException(ErrorCodes.InvalidMatchingOperation, "Bộ lọc trạng thái không hợp lệ.",
                new Dictionary<string, string[]> { ["status"] = ["Trạng thái không hợp lệ."] });
    }
}
