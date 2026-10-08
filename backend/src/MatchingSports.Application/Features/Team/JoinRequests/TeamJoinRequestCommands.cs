using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Enums;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.Team;

public sealed record CreateTeamJoinRequestCommand(Guid ActorUserId, Guid TeamId, string? Message) : IRequest<TeamJoinRequestDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.CreateTeamJoinRequests];
}

public sealed record WithdrawTeamJoinRequestCommand(Guid ActorUserId, Guid TeamId, Guid JoinRequestId, Guid ConcurrencyToken) : IRequest<TeamJoinRequestDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.WithdrawTeamJoinRequests];
}

public sealed record AcceptTeamJoinRequestCommand(Guid ActorUserId, Guid TeamId, Guid JoinRequestId, Guid ConcurrencyToken) : IRequest<TeamJoinRequestDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.AddTeamMembers];
}

public sealed record RejectTeamJoinRequestCommand(Guid ActorUserId, Guid TeamId, Guid JoinRequestId, Guid ConcurrencyToken) : IRequest<TeamJoinRequestDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.AddTeamMembers];
}

public sealed class TeamJoinRequestHandlers(ITeamRepository repository, TeamCommandServices services,
    TeamNotifications notifications, IMapper mapper, TimeProvider clock)
    : IRequestHandler<CreateTeamJoinRequestCommand, TeamJoinRequestDto>, IRequestHandler<WithdrawTeamJoinRequestCommand, TeamJoinRequestDto>,
      IRequestHandler<AcceptTeamJoinRequestCommand, TeamJoinRequestDto>, IRequestHandler<RejectTeamJoinRequestCommand, TeamJoinRequestDto>
{
    public async Task<TeamJoinRequestDto> Handle(CreateTeamJoinRequestCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var team = await services.RequireTeamAsync(request.TeamId, cancellationToken);
        await services.RequireActiveUserAsync(request.ActorUserId, cancellationToken);
        var joinRequest = team.RequestToJoin(request.ActorUserId, request.Message,
            clock.GetUtcNow(), await services.GetSettingsAsync(cancellationToken));
        await notifications.PublishAsync(team, request.ActorUserId, NotificationType.TEAM_JOIN_REQUEST_CREATED,
            team.ManagementUserIds(), cancellationToken, joinRequestId: joinRequest.Id, subjectUserId: joinRequest.UserId);
        await services.SaveAsync(team, transaction, cancellationToken);
        return mapper.Map<TeamJoinRequestDto>(joinRequest);
    }
    public async Task<TeamJoinRequestDto> Handle(WithdrawTeamJoinRequestCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var team = await services.RequireTeamAsync(request.TeamId, cancellationToken, joinRequestId: request.JoinRequestId);
        team.RequireActive();
        var joinRequest = team.JoinRequests.SingleOrDefault(x => x.Id == request.JoinRequestId)
            ?? throw new NotFoundException(ErrorCodes.TeamJoinRequestNotFound, "Không tìm thấy yêu cầu vào đội.");
        TeamCommandRules.RequireOwner(joinRequest.UserId, request.ActorUserId);
        TeamCommandRules.RequireVersion(joinRequest, request.ConcurrencyToken);
        var now = clock.GetUtcNow();

        joinRequest.Withdraw(now);

        await notifications.PublishAsync(team, request.ActorUserId, NotificationType.TEAM_JOIN_REQUEST_WITHDRAWN,
            team.ManagementUserIds(), cancellationToken, joinRequestId: joinRequest.Id, subjectUserId: joinRequest.UserId);
        await services.SaveAsync(team, transaction, cancellationToken);
        return mapper.Map<TeamJoinRequestDto>(joinRequest);
    }
    public async Task<TeamJoinRequestDto> Handle(AcceptTeamJoinRequestCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var team = await services.RequireTeamAsync(request.TeamId, cancellationToken, joinRequestId: request.JoinRequestId);
        team.RequireActive();
        var joinRequest = team.JoinRequests.SingleOrDefault(x => x.Id == request.JoinRequestId)
            ?? throw new NotFoundException(ErrorCodes.TeamJoinRequestNotFound, "Không tìm thấy yêu cầu vào đội.");
        TeamCommandRules.RequireManager(team, request.ActorUserId);
        TeamCommandRules.RequireVersion(joinRequest, request.ConcurrencyToken);
        var now = clock.GetUtcNow();
        joinRequest.RequirePending();
        await services.RequireActiveUserAsync(joinRequest.UserId, cancellationToken);
        team.AddMember(joinRequest.UserId, now, await repository.CountMemberTeamsAsync(joinRequest.UserId, cancellationToken),
            await services.GetSettingsAsync(cancellationToken));
        joinRequest.Accept(request.ActorUserId, now);
        await TeamAdmission.CloseOtherApplicationsAsync(team, joinRequest.UserId, request.ActorUserId, now, notifications, cancellationToken);
        await notifications.PublishAsync(team, request.ActorUserId, NotificationType.TEAM_JOIN_REQUEST_ACCEPTED,
            new[] { joinRequest.UserId }, cancellationToken, joinRequestId: joinRequest.Id, subjectUserId: joinRequest.UserId);
        await services.SaveAsync(team, transaction, cancellationToken);
        return mapper.Map<TeamJoinRequestDto>(joinRequest);
    }
    public async Task<TeamJoinRequestDto> Handle(RejectTeamJoinRequestCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await repository.BeginWriteAsync(cancellationToken);
        var team = await services.RequireTeamAsync(request.TeamId, cancellationToken, joinRequestId: request.JoinRequestId);
        team.RequireActive();
        var joinRequest = team.JoinRequests.SingleOrDefault(x => x.Id == request.JoinRequestId)
            ?? throw new NotFoundException(ErrorCodes.TeamJoinRequestNotFound, "Không tìm thấy yêu cầu vào đội.");
        TeamCommandRules.RequireManager(team, request.ActorUserId);
        TeamCommandRules.RequireVersion(joinRequest, request.ConcurrencyToken);
        var now = clock.GetUtcNow();

        joinRequest.Reject(request.ActorUserId, now);

        await notifications.PublishAsync(team, request.ActorUserId, NotificationType.TEAM_JOIN_REQUEST_REJECTED,
            new[] { joinRequest.UserId }, cancellationToken, joinRequestId: joinRequest.Id, subjectUserId: joinRequest.UserId);
        await services.SaveAsync(team, transaction, cancellationToken);
        return mapper.Map<TeamJoinRequestDto>(joinRequest);
    }
}
