using MatchingSports.Application;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Team;
using MatchingSports.Domain.Entities;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace MatchingSports.Tests;

public sealed class TeamAuthorizationTests
{
    [Fact]
    public async Task EveryTeamUseCaseRejectsMissingPermissionsBeforeResolvingPersistence()
    {
        var services = new ServiceCollection(); services.AddLogging(); services.AddApplication();
        services.AddSingleton<IUserRoleRepository, DeniedActorRepository>();
        using var provider = services.BuildServiceProvider(); using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var actor = Guid.NewGuid(); var team = Guid.NewGuid(); var user = Guid.NewGuid(); var id = Guid.NewGuid(); var token = Guid.NewGuid();
        object[] requests =
        [
            new CreateTeamCommand(actor, id, "Team", null, null, null), new UpdateTeamCommand(actor, team, token, "Team", null, null, null),
            new DeleteTeamCommand(actor, team, token), new TransferTeamCaptainCommand(actor, team, user, token),
            new AppointTeamDeputyCommand(actor, team, user, token), new RevokeTeamDeputyCommand(actor, team, user, token),
            new RemoveTeamMemberCommand(actor, team, user, token), new LeaveTeamCommand(actor, team),
            new InviteTeamMemberCommand(actor, team, user), new CancelTeamInvitationCommand(actor, team, id, token),
            new AcceptTeamInvitationCommand(actor, team, id, token), new RejectTeamInvitationCommand(actor, team, id, token),
            new CreateTeamJoinRequestCommand(actor, team, null), new WithdrawTeamJoinRequestCommand(actor, team, id, token),
            new AcceptTeamJoinRequestCommand(actor, team, id, token), new RejectTeamJoinRequestCommand(actor, team, id, token),
            new GetTeamQuery(actor, team), new GetTeamsQuery { ActorUserId = actor },
            new GetTeamMembersQuery { ActorUserId = actor, TeamId = team },
            new GetTeamInvitationsQuery { ActorUserId = actor }, new GetTeamJoinRequestsQuery { ActorUserId = actor }
        ];
        foreach (var request in requests)
            await Assert.ThrowsAsync<ForbiddenException>(() => mediator.Send(request));
        Assert.Null(scope.ServiceProvider.GetRequiredService<IAuditActorContext>().ActorUserId);
    }
    private sealed class DeniedActorRepository : IUserRoleRepository
    {
        public Task<UserRoleAssignment?> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<UserRoleAssignment?>(new(userId, [], []));
        public Task ReplaceUserRolesAsync(UserRoleAssignment assignment, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
