using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Enums;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Team;

public sealed record CreateTeamCommand(Guid ActorUserId, Guid SportId, string Name, string? Description, string? HomeArea, string? AvatarUrl) : IRequest<TeamDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.CreateTeams];
}

public sealed record UpdateTeamCommand(Guid ActorUserId, Guid TeamId, Guid ConcurrencyToken, string Name, string? Description, string? HomeArea, string? AvatarUrl) : IRequest<TeamDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.UpdateTeams];
}

public sealed record TransferTeamCaptainCommand(Guid ActorUserId, Guid TeamId, Guid UserId, Guid ConcurrencyToken) : IRequest<TeamDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.TransferTeamCaptain];
}

public sealed record DeleteTeamCommand(Guid ActorUserId, Guid TeamId, Guid ConcurrencyToken) : IRequest<TeamDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.DeleteTeams];
}

public sealed class TeamManagementHandlers(ITeamRepository repository, TeamCommandServices services,
    TeamNotifications notifications, IMapper mapper, TimeProvider clock)
    : IRequestHandler<CreateTeamCommand, TeamDto>, IRequestHandler<UpdateTeamCommand, TeamDto>,
      IRequestHandler<TransferTeamCaptainCommand, TeamDto>, IRequestHandler<DeleteTeamCommand, TeamDto>
{
    public async Task<TeamDto> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        await services.RequireActiveUserAsync(request.ActorUserId, cancellationToken);
        if (!await repository.IsActiveSportAsync(request.SportId, cancellationToken))
            throw new NotFoundException(ErrorCodes.SportNotFound, "Không tìm thấy môn thể thao đang hoạt động.");
        var settings = await services.GetSettingsAsync(cancellationToken);
        var count = await repository.CountCaptainTeamsAsync(request.ActorUserId, cancellationToken);
        var team = Domain.Entities.Team.Create(request.SportId, request.ActorUserId, request.Name, request.Description,
            request.HomeArea, request.AvatarUrl, clock.GetUtcNow(), count, settings);
        repository.Add(team);
        await notifications.PublishAsync(team, request.ActorUserId, NotificationType.TEAM_CREATED, [request.ActorUserId], cancellationToken, includeActor: true);
        await services.SaveAsync(team, transaction, cancellationToken);
        return mapper.Map<TeamDto>(team);
    }
    public async Task<TeamDto> Handle(UpdateTeamCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var team = await services.RequireTeamAsync(request.TeamId, cancellationToken);
        TeamCommandRules.RequireCaptain(team, request.ActorUserId);
        TeamCommandRules.RequireVersion(team, request.ConcurrencyToken);
        team.UpdateDetails(request.Name, request.Description, request.HomeArea, request.AvatarUrl);
        await notifications.PublishAsync(team, request.ActorUserId, NotificationType.TEAM_UPDATED, team.ActiveUserIds(), cancellationToken);
        await services.SaveAsync(team, transaction, cancellationToken);
        return mapper.Map<TeamDto>(team);
    }
    public async Task<TeamDto> Handle(TransferTeamCaptainCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var team = await services.RequireTeamAsync(request.TeamId, cancellationToken);
        TeamCommandRules.RequireCaptain(team, request.ActorUserId);
        TeamCommandRules.RequireVersion(team, request.ConcurrencyToken);
        await services.RequireActiveUserAsync(request.UserId, cancellationToken);
        var settings = await services.GetSettingsAsync(cancellationToken);
        var captainCount = await repository.CountCaptainTeamsAsync(request.UserId, cancellationToken);
        var memberCount = await repository.CountMemberTeamsAsync(request.ActorUserId, cancellationToken);
        team.TransferCaptain(request.UserId, captainCount, memberCount, settings);
        await notifications.PublishAsync(team, request.ActorUserId, NotificationType.TEAM_CAPTAIN_TRANSFERRED, team.ActiveUserIds(), cancellationToken, subjectUserId: request.UserId);
        await services.SaveAsync(team, transaction, cancellationToken);
        return mapper.Map<TeamDto>(team);
    }
    public async Task<TeamDto> Handle(DeleteTeamCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var team = await services.RequireTeamAsync(request.TeamId, cancellationToken);
        TeamCommandRules.RequireCaptain(team, request.ActorUserId);
        TeamCommandRules.RequireVersion(team, request.ConcurrencyToken);
        team.Delete(clock.GetUtcNow(), await repository.HasOpenCommitmentsAsync(team.Id, cancellationToken));
        var affectedUserIds = new HashSet<Guid> { request.ActorUserId };
        foreach (var invitation in team.Invitations.Where(x => x.Status == TeamRequestStatus.PENDING))
        {
            invitation.Cancel(clock.GetUtcNow());
            affectedUserIds.Add(invitation.UserId);
        }
        foreach (var joinRequest in team.JoinRequests.Where(x => x.Status == TeamRequestStatus.PENDING))
        {
            joinRequest.Cancel(clock.GetUtcNow());
            affectedUserIds.Add(joinRequest.UserId);
        }
        await notifications.PublishAsync(team, request.ActorUserId, NotificationType.TEAM_DELETED, affectedUserIds, cancellationToken, includeActor: true);
        await services.SaveAsync(team, transaction, cancellationToken);
        return mapper.Map<TeamDto>(team);
    }
}
