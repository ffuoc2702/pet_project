using AutoMapper;
using MatchingSports.Api.Contracts;
using MatchingSports.Application;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Notification;
using MatchingSports.Application.Features.Team;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Infrastructure;
using MatchingSports.Infrastructure.Persistence;
using MatchingSports.Shared;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace MatchingSports.Tests;

public sealed class TeamPersistenceTests
{
    [RbacDatabaseFact]
    public async Task TeamWorkflowEnforcesOwnershipRolesVersionsAndAtomicNotifications()
    {
        var connection = TestConnection(); var sender = new TeamEmailSender();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:matchingsports"] = connection.ConnectionString,
            ["Auth:Jwt:SigningKey"] = new string('s', 48),
            ["Auth:Otp:Pepper"] = new string('p', 48),
            ["Teams:Notifications:EmailEnabled"] = "true"
        }).Build();
        var services = new ServiceCollection(); services.AddLogging(); services.AddSingleton<IConfiguration>(configuration);
        services.AddApplication(null, typeof(ContractMappingProfile).Assembly); services.AddInfrastructure(configuration);
        services.AddSingleton<INotificationEmailSender>(sender);
        await using var provider = services.BuildServiceProvider();
        try
        {
            await provider.InitializeDatabaseAsync(true);
            Guid captain, member, successor, applicant, sportId;
            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
                var manager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                var ids = new List<Guid>();
                foreach (var name in new[] { "captain", "member", "successor", "applicant" })
                {
                    var user = User.CreateVerifiedAccount($"team-{name}@example.test", name);
                    Assert.True((await manager.CreateAsync(user, "Testing-Password123!")).Succeeded);
                    Assert.True((await manager.AddToRoleAsync(user, RoleNames.User)).Succeeded); ids.Add(user.Id);
                }
                captain = ids[0]; member = ids[1]; successor = ids[2]; applicant = ids[3];
                sportId = (await db.Sports.FirstAsync()).Id;
                scope.ServiceProvider.GetRequiredService<IMapper>().ConfigurationProvider.AssertConfigurationIsValid();
            }
            var team = await SendAsync(provider, new CreateTeamCommand(captain, sportId, "  Team  ", null, null, null));
            Assert.Equal(captain, team.CreatedBy); Assert.Equal("Team", team.Name);
            var request = await SendAsync(provider, new CreateTeamJoinRequestCommand(member, team.Id, "Hello"));
            var invitation = await SendAsync(provider, new InviteTeamMemberCommand(captain, team.Id, member));
            await Assert.ThrowsAsync<ForbiddenException>(() => SendAsync(provider,
                new AcceptTeamInvitationCommand(successor, team.Id, invitation.Id, invitation.ConcurrencyToken)));
            await Assert.ThrowsAsync<ConflictException>(() => SendAsync(provider,
                new DeleteTeamCommand(captain, team.Id, team.ConcurrencyToken)));
            await SendAsync(provider, new AcceptTeamInvitationCommand(member, team.Id, invitation.Id, invitation.ConcurrencyToken));
            await Assert.ThrowsAsync<ConflictException>(() => SendAsync(provider,
                new AcceptTeamInvitationCommand(member, team.Id, invitation.Id, invitation.ConcurrencyToken)));
            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
                Assert.Equal(TeamRequestStatus.CANCELLED, (await db.TeamJoinRequests.SingleAsync(x => x.Id == request.Id)).Status);
                var accepted = await db.NotificationHistories.SingleAsync(x => x.Type == NotificationType.TEAM_INVITATION_ACCEPTED);
                Assert.Equal(captain, accepted.UserId); Assert.Equal(member, accepted.CreatedBy);
                Assert.Null(accepted.NotificationConfigId); Assert.Equal(team.Id, accepted.TeamId);
                Assert.Equal(accepted.CreatedAt.AddDays(90), accepted.PurgeAfter);
            }
            team = await SendAsync(provider, new GetTeamQuery(captain, team.Id));
            await Assert.ThrowsAsync<ConflictException>(() => SendAsync(provider, new DeleteTeamCommand(captain, team.Id, team.ConcurrencyToken)));
            team = await SendAsync(provider, new AppointTeamDeputyCommand(captain, team.Id, member, team.ConcurrencyToken));
            await Assert.ThrowsAsync<ForbiddenException>(() => SendAsync(provider,
                new UpdateTeamCommand(member, team.Id, team.ConcurrencyToken, "Forbidden", null, null, null)));
            invitation = await SendAsync(provider, new InviteTeamMemberCommand(member, team.Id, successor));
            await SendAsync(provider, new AcceptTeamInvitationCommand(successor, team.Id, invitation.Id, invitation.ConcurrencyToken));
            team = await SendAsync(provider, new GetTeamQuery(captain, team.Id));
            team = await SendAsync(provider, new TransferTeamCaptainCommand(captain, team.Id, successor, team.ConcurrencyToken));
            Assert.Equal(successor, team.CaptainUserId);
            await Assert.ThrowsAsync<ForbiddenException>(() => SendAsync(provider,
                new TransferTeamCaptainCommand(captain, team.Id, member, team.ConcurrencyToken)));
            var members = await SendAsync(provider, new GetTeamMembersQuery { ActorUserId = successor, TeamId = team.Id, PageSize = 1 });
            Assert.Equal(3, members.TotalItems); Assert.Single(members.Items);
            await SendAsync(provider, new LeaveTeamCommand(member, team.Id));
            team = await SendAsync(provider, new GetTeamQuery(successor, team.Id));
            await SendAsync(provider, new RemoveTeamMemberCommand(successor, team.Id, captain, team.ConcurrencyToken));
            await SendAsync(provider, new InviteTeamMemberCommand(successor, team.Id, applicant));
            await SendAsync(provider, new CreateTeamJoinRequestCommand(applicant, team.Id, null));
            team = await SendAsync(provider, new GetTeamQuery(successor, team.Id));
            team = await SendAsync(provider, new DeleteTeamCommand(successor, team.Id, team.ConcurrencyToken));
            Assert.NotNull(team.DeletedAt);
            await Assert.ThrowsAsync<NotFoundException>(() => SendAsync(provider, new GetTeamQuery(successor, team.Id)));
            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
                Assert.DoesNotContain(await db.TeamInvitations.IgnoreQueryFilters().ToListAsync(), x => x.Status == TeamRequestStatus.PENDING);
                Assert.DoesNotContain(await db.TeamJoinRequests.IgnoreQueryFilters().ToListAsync(), x => x.Status == TeamRequestStatus.PENDING);
                Assert.Equal(1, await db.NotificationHistories.CountAsync(x => x.Type == NotificationType.TEAM_DELETED && x.UserId == applicant));
                Assert.Equal(1, await db.NotificationHistories.CountAsync(x => x.Type == NotificationType.TEAM_DELETED && x.UserId == successor));
            }
            while (await DispatchAsync(provider)) { }
            Assert.True(sender.SentCount > 0);
            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
                Assert.All(await db.NotificationHistories.ToListAsync(), x => Assert.Equal(NotificationDeliveryStatus.SENT, x.EmailStatus));
                Assert.All(await db.OutboxMessages.ToListAsync(), x => Assert.NotNull(x.ProcessedAt));
            }
        }
        finally { await DeleteDatabaseAsync(provider); }
    }

    [RbacDatabaseFact]
    public async Task ConcurrentAdmissionsCannotOverfillTheLastPlaceOrExceedUserQuota()
    {
        var connection = TestConnection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:matchingsports"] = connection.ConnectionString,
            ["Auth:Jwt:SigningKey"] = new string('s', 48),
            ["Auth:Otp:Pepper"] = new string('p', 48)
        }).Build();
        var services = new ServiceCollection(); services.AddLogging(); services.AddSingleton<IConfiguration>(configuration);
        services.AddApplication(); services.AddInfrastructure(configuration);
        await using var provider = services.BuildServiceProvider();
        try
        {
            await provider.InitializeDatabaseAsync(true);
            var users = new List<Guid>(); Guid sportId;
            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
                var manager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                for (var index = 0; index < 4; index++)
                {
                    var user = User.CreateVerifiedAccount($"quota-{index}@example.test", $"User {index}");
                    Assert.True((await manager.CreateAsync(user, "Testing-Password123!")).Succeeded);
                    Assert.True((await manager.AddToRoleAsync(user, RoleNames.User)).Succeeded); users.Add(user.Id);
                }
                sportId = (await db.Sports.FirstAsync()).Id;
                var settings = await db.SystemSettings.SingleAsync();
                settings.UpdateValues(settings.GetValues() with { MaxMembersPerTeam = 2, MaxDeputiesPerTeam = 0, MaxMemberTeamsPerUser = 1 });
                await db.SaveChangesAsync();
            }
            var first = await SendAsync(provider, new CreateTeamCommand(users[0], sportId, "First", null, null, null));
            var a = await SendAsync(provider, new InviteTeamMemberCommand(users[0], first.Id, users[2]));
            var b = await SendAsync(provider, new InviteTeamMemberCommand(users[0], first.Id, users[3]));
            var results = await Task.WhenAll(TryAcceptAsync(provider, a), TryAcceptAsync(provider, b));
            Assert.Single(results, x => x);
            var winner = results[0] ? users[2] : users[3];
            var second = await SendAsync(provider, new CreateTeamCommand(users[1], sportId, "Second", null, null, null));
            var third = await SendAsync(provider, new CreateTeamCommand(users[1], sportId, "Third", null, null, null));
            await SendAsync(provider, new LeaveTeamCommand(winner, first.Id));
            a = await SendAsync(provider, new InviteTeamMemberCommand(users[1], second.Id, winner));
            b = await SendAsync(provider, new InviteTeamMemberCommand(users[1], third.Id, winner));
            results = await Task.WhenAll(TryAcceptAsync(provider, a), TryAcceptAsync(provider, b));
            Assert.Single(results, x => x);
            await using var scope2 = provider.CreateAsyncScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
            Assert.Equal(1, await db2.TeamMembers.CountAsync(x => x.UserId == winner && x.Status == MemberStatus.ACTIVE));
            Assert.Equal(2, await db2.NotificationHistories.CountAsync(x => x.Type == NotificationType.TEAM_INVITATION_ACCEPTED));
        }
        finally { await DeleteDatabaseAsync(provider); }
    }
    [RbacDatabaseFact]
    public async Task DeletionAndAdmissionCannotBothCommitAndExpiredInvitationsCanBeReplaced()
    {
        var connection = TestConnection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:matchingsports"] = connection.ConnectionString,
            ["Auth:Jwt:SigningKey"] = new string('s', 48),
            ["Auth:Otp:Pepper"] = new string('p', 48)
        }).Build();
        var services = new ServiceCollection(); services.AddLogging(); services.AddSingleton<IConfiguration>(configuration);
        services.AddApplication(); services.AddInfrastructure(configuration);
        await using var provider = services.BuildServiceProvider();
        try
        {
            await provider.InitializeDatabaseAsync(true);
            var users = new List<Guid>(); Guid sportId;
            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
                var manager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                for (var index = 0; index < 2; index++)
                {
                    var user = User.CreateVerifiedAccount($"race-{index}@example.test", $"User {index}");
                    Assert.True((await manager.CreateAsync(user, "Testing-Password123!")).Succeeded);
                    Assert.True((await manager.AddToRoleAsync(user, RoleNames.User)).Succeeded); users.Add(user.Id);
                }
                sportId = (await db.Sports.FirstAsync()).Id;
            }
            var team = await SendAsync(provider, new CreateTeamCommand(users[0], sportId, "Race", null, null, null));
            var invitation = await SendAsync(provider, new InviteTeamMemberCommand(users[0], team.Id, users[1]));
            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
                await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE team_invitations SET expires_at = {DateTimeOffset.UtcNow.AddHours(-1)} WHERE id = {invitation.Id}");
            }
            var expiredPage = await SendAsync(provider, new GetTeamInvitationsQuery { ActorUserId = users[1], Status = TeamRequestStatus.EXPIRED });
            Assert.Equal(TeamRequestStatus.EXPIRED, Assert.Single(expiredPage.Items).Status);
            invitation = await SendAsync(provider, new InviteTeamMemberCommand(users[0], team.Id, users[1]));
            team = await SendAsync(provider, new GetTeamQuery(users[0], team.Id));
            var results = await Task.WhenAll(TryDeleteAsync(provider, users[0], team), TryAcceptAsync(provider, invitation));
            Assert.Single(results, x => x);
            await using var scope2 = provider.CreateAsyncScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
            var stored = await db2.Teams.IgnoreQueryFilters().SingleAsync(x => x.Id == team.Id);
            if (stored.DeletedAt is null)
            {
                Assert.Equal(2, await db2.TeamMembers.CountAsync(x => x.TeamId == team.Id && x.Status == MemberStatus.ACTIVE));
                Assert.Equal(0, await db2.NotificationHistories.CountAsync(x => x.Type == NotificationType.TEAM_DELETED));
            }
            else
            {
                Assert.Equal(1, await db2.TeamMembers.IgnoreQueryFilters().CountAsync(x => x.TeamId == team.Id && x.Status == MemberStatus.ACTIVE));
                Assert.Equal(0, await db2.NotificationHistories.CountAsync(x => x.Type == NotificationType.TEAM_INVITATION_ACCEPTED));
            }
        }
        finally { await DeleteDatabaseAsync(provider); }
    }
    private static async Task<bool> TryDeleteAsync(ServiceProvider provider, Guid actorId, TeamDto team)
    {
        try { await SendAsync(provider, new DeleteTeamCommand(actorId, team.Id, team.ConcurrencyToken)); return true; }
        catch (ConflictException) { return false; }
        catch (NotFoundException) { return false; }
    }
    private static async Task<bool> TryAcceptAsync(ServiceProvider provider, TeamInvitationDto invitation)
    {
        try { await SendAsync(provider, new AcceptTeamInvitationCommand(invitation.UserId, invitation.TeamId, invitation.Id, invitation.ConcurrencyToken)); return true; }
        catch (ConflictException) { return false; }
        catch (NotFoundException) { return false; }
    }
    private static async Task<T> SendAsync<T>(ServiceProvider provider, IRequest<T> request)
    {
        await using var scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request);
    }
    private static async Task<bool> DispatchAsync(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<NotificationJobs>().DispatchNextEmailAsync(CancellationToken.None);
    }
    private static NpgsqlConnectionStringBuilder TestConnection()
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("MATCHINGSPORTS_RBAC_TEST_CONNECTION")!);
        Assert.StartsWith("matchingsports_rbac_test_", connection.Database, StringComparison.Ordinal);
        connection.Database = $"matchingsports_rbac_test_team_{Guid.NewGuid():N}";
        return connection;
    }
    private static async Task DeleteDatabaseAsync(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>().Database.EnsureDeletedAsync();
        NpgsqlConnection.ClearAllPools();
    }
    private sealed class TeamEmailSender : INotificationEmailSender
    {
        public bool IsConfigured => true;
        public int SentCount { get; private set; }
        public Task SendAsync(string email, string title, string html, string idempotencyKey, CancellationToken cancellationToken)
        { SentCount++; return Task.CompletedTask; }
    }
}
