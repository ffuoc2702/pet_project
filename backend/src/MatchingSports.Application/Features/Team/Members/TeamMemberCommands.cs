using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Enums;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Team;

public sealed record AppointTeamDeputyCommand(Guid ActorUserId, Guid TeamId, Guid UserId, Guid ConcurrencyToken) : IRequest<TeamDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ManageTeamDeputies];
}

public sealed record RevokeTeamDeputyCommand(Guid ActorUserId, Guid TeamId, Guid UserId, Guid ConcurrencyToken) : IRequest<TeamDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ManageTeamDeputies];
}

public sealed record RemoveTeamMemberCommand(Guid ActorUserId, Guid TeamId, Guid UserId, Guid ConcurrencyToken) : IRequest<TeamDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.RemoveTeamMembers];
}

public sealed record LeaveTeamCommand(Guid ActorUserId, Guid TeamId) : IRequest<TeamDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.LeaveTeams];
}

public sealed class TeamMemberHandlers(ITeamRepository repository, TeamCommandServices services,
    TeamNotifications notifications, IMapper mapper, TimeProvider clock)
    : IRequestHandler<AppointTeamDeputyCommand, TeamDto>, IRequestHandler<RevokeTeamDeputyCommand, TeamDto>,
      IRequestHandler<RemoveTeamMemberCommand, TeamDto>, IRequestHandler<LeaveTeamCommand, TeamDto>
{
    public async Task<TeamDto> Handle(AppointTeamDeputyCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var team = await services.RequireTeamAsync(request.TeamId, cancellationToken);
        TeamCommandRules.RequireCaptain(team, request.ActorUserId);
        TeamCommandRules.RequireVersion(team, request.ConcurrencyToken);
        await services.RequireActiveUserAsync(request.UserId, cancellationToken);
        team.AppointDeputy(request.UserId, await services.GetSettingsAsync(cancellationToken));
        await notifications.PublishAsync(team, request.ActorUserId, NotificationType.TEAM_DEPUTY_APPOINTED,
            team.ManagementUserIds().Append(request.UserId), cancellationToken, subjectUserId: request.UserId);
        await services.SaveAsync(team, transaction, cancellationToken);
        return mapper.Map<TeamDto>(team);
    }
    public async Task<TeamDto> Handle(RevokeTeamDeputyCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var team = await services.RequireTeamAsync(request.TeamId, cancellationToken);
        TeamCommandRules.RequireCaptain(team, request.ActorUserId);
        TeamCommandRules.RequireVersion(team, request.ConcurrencyToken);

        team.RevokeDeputy(request.UserId);
        await notifications.PublishAsync(team, request.ActorUserId, NotificationType.TEAM_DEPUTY_REVOKED,
            team.ManagementUserIds().Append(request.UserId), cancellationToken, subjectUserId: request.UserId);
        await services.SaveAsync(team, transaction, cancellationToken);
        return mapper.Map<TeamDto>(team);
    }
    public async Task<TeamDto> Handle(RemoveTeamMemberCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var team = await services.RequireTeamAsync(request.TeamId, cancellationToken);
        TeamCommandRules.RequireManager(team, request.ActorUserId);
        TeamCommandRules.RequireVersion(team, request.ConcurrencyToken);

        team.RemoveMember(request.ActorUserId, request.UserId, clock.GetUtcNow());
        await notifications.PublishAsync(team, request.ActorUserId, NotificationType.TEAM_MEMBER_REMOVED,
            team.ManagementUserIds().Append(request.UserId), cancellationToken, subjectUserId: request.UserId);
        await services.SaveAsync(team, transaction, cancellationToken);
        return mapper.Map<TeamDto>(team);
    }
    public async Task<TeamDto> Handle(LeaveTeamCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var team = await services.RequireTeamAsync(request.TeamId, cancellationToken);
        team.Leave(request.ActorUserId, clock.GetUtcNow());
        await notifications.PublishAsync(team, request.ActorUserId, NotificationType.TEAM_MEMBER_LEFT, team.ManagementUserIds(), cancellationToken, subjectUserId: request.ActorUserId);
        await services.SaveAsync(team, transaction, cancellationToken);
        return mapper.Map<TeamDto>(team);
    }
}
