using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Enums;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Team;

public sealed record InviteTeamMemberCommand(Guid ActorUserId, Guid TeamId, Guid UserId) : IRequest<TeamInvitationDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.AddTeamMembers];
}

public sealed record CancelTeamInvitationCommand(Guid ActorUserId, Guid TeamId, Guid InvitationId, Guid ConcurrencyToken) : IRequest<TeamInvitationDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.AddTeamMembers];
}

public sealed record AcceptTeamInvitationCommand(Guid ActorUserId, Guid TeamId, Guid InvitationId, Guid ConcurrencyToken) : IRequest<TeamInvitationDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.RespondTeamInvitations];
}

public sealed record RejectTeamInvitationCommand(Guid ActorUserId, Guid TeamId, Guid InvitationId, Guid ConcurrencyToken) : IRequest<TeamInvitationDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.RespondTeamInvitations];
}

public sealed class TeamInvitationHandlers(ITeamRepository repository, TeamCommandServices services,
    TeamNotifications notifications, IMapper mapper, TimeProvider clock)
    : IRequestHandler<InviteTeamMemberCommand, TeamInvitationDto>, IRequestHandler<CancelTeamInvitationCommand, TeamInvitationDto>,
      IRequestHandler<AcceptTeamInvitationCommand, TeamInvitationDto>, IRequestHandler<RejectTeamInvitationCommand, TeamInvitationDto>
{
    public async Task<TeamInvitationDto> Handle(InviteTeamMemberCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var team = await services.RequireTeamAsync(request.TeamId, cancellationToken);
        TeamCommandRules.RequireManager(team, request.ActorUserId);
        await services.RequireActiveUserAsync(request.UserId, cancellationToken);
        var invitation = team.Invite(request.ActorUserId, request.UserId, clock.GetUtcNow(), await services.GetSettingsAsync(cancellationToken));
        await notifications.PublishAsync(team, request.ActorUserId, NotificationType.TEAM_INVITATION_CREATED,
            [request.UserId], cancellationToken, invitationId: invitation.Id, subjectUserId: invitation.UserId);
        await services.SaveAsync(team, transaction, cancellationToken);
        return mapper.Map<TeamInvitationDto>(invitation);
    }
    public async Task<TeamInvitationDto> Handle(CancelTeamInvitationCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var team = await services.RequireTeamAsync(request.TeamId, cancellationToken, invitationId: request.InvitationId);
        team.RequireActive();
        var invitation = team.Invitations.SingleOrDefault(x => x.Id == request.InvitationId)
            ?? throw new NotFoundException(ErrorCodes.TeamInvitationNotFound, "Không tìm thấy lời mời.");
        TeamCommandRules.RequireManager(team, request.ActorUserId);
        TeamCommandRules.RequireVersion(invitation, request.ConcurrencyToken);
        var now = clock.GetUtcNow();

        invitation.Cancel(now);

        await notifications.PublishAsync(team, request.ActorUserId, NotificationType.TEAM_INVITATION_CANCELLED,
            new[] { invitation.UserId }, cancellationToken, invitationId: invitation.Id, subjectUserId: invitation.UserId);
        await services.SaveAsync(team, transaction, cancellationToken);
        return mapper.Map<TeamInvitationDto>(invitation);
    }
    public async Task<TeamInvitationDto> Handle(AcceptTeamInvitationCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var team = await services.RequireTeamAsync(request.TeamId, cancellationToken, invitationId: request.InvitationId);
        team.RequireActive();
        var invitation = team.Invitations.SingleOrDefault(x => x.Id == request.InvitationId)
            ?? throw new NotFoundException(ErrorCodes.TeamInvitationNotFound, "Không tìm thấy lời mời.");
        TeamCommandRules.RequireOwner(invitation.UserId, request.ActorUserId);
        TeamCommandRules.RequireVersion(invitation, request.ConcurrencyToken);
        var now = clock.GetUtcNow();
        invitation.RequirePending(now);
        await services.RequireActiveUserAsync(request.ActorUserId, cancellationToken);
        team.AddMember(invitation.UserId, now, await repository.CountMemberTeamsAsync(invitation.UserId, cancellationToken),
            await services.GetSettingsAsync(cancellationToken));
        invitation.Accept(now);
        await TeamAdmission.CloseOtherApplicationsAsync(team, invitation.UserId, request.ActorUserId, now, notifications, cancellationToken);
        await notifications.PublishAsync(team, request.ActorUserId, NotificationType.TEAM_INVITATION_ACCEPTED,
            team.ManagementUserIds().Append(invitation.InviterUserId).Append(invitation.UserId), cancellationToken, invitationId: invitation.Id, subjectUserId: invitation.UserId);
        await services.SaveAsync(team, transaction, cancellationToken);
        return mapper.Map<TeamInvitationDto>(invitation);
    }
    public async Task<TeamInvitationDto> Handle(RejectTeamInvitationCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var team = await services.RequireTeamAsync(request.TeamId, cancellationToken, invitationId: request.InvitationId);
        team.RequireActive();
        var invitation = team.Invitations.SingleOrDefault(x => x.Id == request.InvitationId)
            ?? throw new NotFoundException(ErrorCodes.TeamInvitationNotFound, "Không tìm thấy lời mời.");
        TeamCommandRules.RequireOwner(invitation.UserId, request.ActorUserId);
        TeamCommandRules.RequireVersion(invitation, request.ConcurrencyToken);
        var now = clock.GetUtcNow();

        invitation.Reject(now);

        await notifications.PublishAsync(team, request.ActorUserId, NotificationType.TEAM_INVITATION_REJECTED,
            team.ManagementUserIds().Append(invitation.InviterUserId).Append(invitation.UserId), cancellationToken, invitationId: invitation.Id, subjectUserId: invitation.UserId);
        await services.SaveAsync(team, transaction, cancellationToken);
        return mapper.Map<TeamInvitationDto>(invitation);
    }
}
