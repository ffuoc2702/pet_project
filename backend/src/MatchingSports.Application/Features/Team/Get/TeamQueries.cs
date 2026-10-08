using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Enums;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Team;

public sealed record GetTeamsQuery : BaseQuery<TeamDto>, IPermissionAuthorizedRequest
{
    public Guid ActorUserId { get; init; }
    public bool MineOnly { get; init; }
    public Guid? SportId { get; init; }
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ListTeams];
}

public sealed record GetTeamMembersQuery : BaseQuery<TeamMemberDto>, IPermissionAuthorizedRequest
{
    public Guid ActorUserId { get; init; }
    public Guid TeamId { get; init; }
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ListTeams];
}

public sealed record GetTeamInvitationsQuery : BaseQuery<TeamInvitationDto>, IPermissionAuthorizedRequest
{
    public Guid ActorUserId { get; init; }
    public Guid? TeamId { get; init; }
    public TeamRequestStatus? Status { get; init; }
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ListTeams];
}

public sealed record GetTeamJoinRequestsQuery : BaseQuery<TeamJoinRequestDto>, IPermissionAuthorizedRequest
{
    public Guid ActorUserId { get; init; }
    public Guid? TeamId { get; init; }
    public TeamRequestStatus? Status { get; init; }
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ListTeams];
}

public sealed record GetTeamQuery(Guid ActorUserId, Guid TeamId) : IRequest<TeamDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ReadTeams];
}

public sealed class TeamQueryHandlers(ITeamRepository repository, IMapper mapper, TimeProvider clock)
    : IRequestHandler<GetTeamsQuery, PaginatedResult<TeamDto>>,
      IRequestHandler<GetTeamQuery, TeamDto>,
      IRequestHandler<GetTeamMembersQuery, PaginatedResult<TeamMemberDto>>,
      IRequestHandler<GetTeamInvitationsQuery, PaginatedResult<TeamInvitationDto>>,
      IRequestHandler<GetTeamJoinRequestsQuery, PaginatedResult<TeamJoinRequestDto>>
{
    public async Task<PaginatedResult<TeamDto>> Handle(GetTeamsQuery request, CancellationToken cancellationToken) =>
        (await repository.ListAsync(request, cancellationToken)).Map(mapper.Map<TeamDto>);
    public async Task<TeamDto> Handle(GetTeamQuery request, CancellationToken cancellationToken)
    {
        var team = await RequireTeamAsync(request.TeamId, cancellationToken);
        if (team.Status != TeamStatus.ACTIVE && !team.IsMember(request.ActorUserId) && !team.IsCaptain(request.ActorUserId))
            throw new ForbiddenException(ErrorCodes.Forbidden, "Không được truy cập đội này.");
        return mapper.Map<TeamDto>(team);
    }
    public async Task<PaginatedResult<TeamMemberDto>> Handle(GetTeamMembersQuery request, CancellationToken cancellationToken)
    {
        var team = await RequireTeamAsync(request.TeamId, cancellationToken);
        if (!team.IsMember(request.ActorUserId) && !team.IsCaptain(request.ActorUserId))
            throw new ForbiddenException(ErrorCodes.Forbidden, "Chỉ thành viên được xem danh sách thành viên đội.");
        return (await repository.ListMembersAsync(request, cancellationToken)).Map(mapper.Map<TeamMemberDto>);
    }
    public async Task<PaginatedResult<TeamInvitationDto>> Handle(GetTeamInvitationsQuery request, CancellationToken cancellationToken)
    {
        if (request.TeamId is { } teamId) TeamCommandRules.RequireManager(await RequireTeamAsync(teamId, cancellationToken), request.ActorUserId);
        return (await repository.ListInvitationsAsync(request, clock.GetUtcNow(), cancellationToken)).Map(mapper.Map<TeamInvitationDto>);
    }
    public async Task<PaginatedResult<TeamJoinRequestDto>> Handle(GetTeamJoinRequestsQuery request, CancellationToken cancellationToken)
    {
        if (request.TeamId is { } teamId) TeamCommandRules.RequireManager(await RequireTeamAsync(teamId, cancellationToken), request.ActorUserId);
        return (await repository.ListJoinRequestsAsync(request, cancellationToken)).Map(mapper.Map<TeamJoinRequestDto>);
    }
    private async Task<Domain.Entities.Team> RequireTeamAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException(ErrorCodes.TeamNotFound, "Không tìm thấy đội.");
}
