using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Domain.ValueObjects;

namespace MatchingSports.Tests;

public sealed class TeamDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 0, 0, 0, TimeSpan.Zero);
    private static Team NewTeam(SystemSettings? settings = null) => Team.Create(Guid.NewGuid(), Guid.NewGuid(),
        "  Football  ", "   ", null, null, Now, 0, settings ?? SystemSettings.CreateDefaults());

    [Fact]
    public void CreationNormalizesDetailsAndCreatesCaptainMembership()
    {
        var team = NewTeam();
        Assert.Equal("Football", team.Name); Assert.Null(team.Description);
        var captain = Assert.Single(team.Members);
        Assert.Equal(team.CaptainUserId, captain.UserId); Assert.Equal(TeamRole.CAPTAIN, captain.Role);
        Assert.NotEqual(Guid.Empty, team.ConcurrencyToken);
    }
    [Fact]
    public void InvalidDetailsLeaveExistingValuesUnchanged()
    {
        var team = NewTeam(); var token = team.ConcurrencyToken;
        Assert.Throws<TeamRuleException>(() => team.UpdateDetails(" ", null, null, null));
        Assert.Equal("Football", team.Name); Assert.Equal(token, team.ConcurrencyToken);
    }
    [Fact]
    public void TransferMovesBothRolesAndFreesDeputyPosition()
    {
        var settings = SystemSettings.CreateDefaults(); var team = NewTeam(); var oldCaptain = team.CaptainUserId;
        var successor = Guid.NewGuid(); team.AddMember(successor, Now, 0, settings); team.AppointDeputy(successor, settings);
        team.TransferCaptain(successor, 0, 0, settings);
        Assert.Equal(successor, team.CaptainUserId);
        Assert.Equal(TeamRole.MEMBER, team.Members.Single(x => x.UserId == oldCaptain).Role);
        Assert.Equal(successor, Assert.Single(team.Members, x => x.Role == TeamRole.CAPTAIN).UserId);
        Assert.DoesNotContain(team.Members, x => x.Role == TeamRole.DEPUTY);
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TransferChecksBothUsersQuotasBeforeChangingAnyRole(bool captainQuota)
    {
        var settings = SystemSettings.CreateDefaults(); var team = NewTeam(); var oldCaptain = team.CaptainUserId;
        var successor = Guid.NewGuid(); team.AddMember(successor, Now, 0, settings); var token = team.ConcurrencyToken;
        Assert.Throws<TeamRuleException>(() => team.TransferCaptain(successor,
            captainQuota ? settings.MaxCaptainTeamsPerUser : 0, captainQuota ? 0 : settings.MaxMemberTeamsPerUser, settings));
        Assert.Equal(oldCaptain, team.CaptainUserId); Assert.Equal(token, team.ConcurrencyToken);
        Assert.Equal(TeamRole.MEMBER, team.Members.Single(x => x.UserId == successor).Role);
    }
    [Fact]
    public void CaptainCannotLeaveOrBeRemovedAndOutsiderCannotBecomeCaptain()
    {
        var team = NewTeam(); var settings = SystemSettings.CreateDefaults();
        Assert.Throws<TeamRuleException>(() => team.Leave(team.CaptainUserId, Now));
        Assert.Throws<TeamRuleException>(() => team.RemoveMember(team.CaptainUserId, team.CaptainUserId, Now));
        Assert.Throws<TeamRuleException>(() => team.TransferCaptain(Guid.NewGuid(), 0, 0, settings));
    }
    [Fact]
    public void DeputyCannotRemoveAnotherDeputy()
    {
        var team = NewTeam(); var settings = SystemSettings.CreateDefaults(); var first = Guid.NewGuid(); var second = Guid.NewGuid();
        team.AddMember(first, Now, 0, settings); team.AddMember(second, Now, 0, settings);
        team.AppointDeputy(first, settings); team.AppointDeputy(second, settings);
        Assert.Throws<TeamRuleException>(() => team.RemoveMember(first, second, Now));
        Assert.True(team.IsMember(second));
    }
    [Fact]
    public void MembershipCanRejoinWithoutAddingDuplicateCompositeKey()
    {
        var team = NewTeam(); var settings = SystemSettings.CreateDefaults(); var userId = Guid.NewGuid();
        team.AddMember(userId, Now, 0, settings); team.AppointDeputy(userId, settings); team.Leave(userId, Now);
        team.AddMember(userId, Now.AddDays(1), 0, settings);
        var member = Assert.Single(team.Members, x => x.UserId == userId);
        Assert.Equal(TeamRole.MEMBER, member.Role); Assert.Null(member.LeftAt);
        Assert.Equal(Now.AddDays(1), member.JoinedAt);
    }
    [Fact]
    public void AdmissionChecksCapacityAndUserQuotaBeforeAddingMember()
    {
        var settings = SystemSettings.CreateDefaults(); settings.UpdateValues(settings.GetValues() with { MaxMembersPerTeam = 1, MaxDeputiesPerTeam = 0 });
        var team = NewTeam(settings);
        Assert.Throws<TeamRuleException>(() => team.AddMember(Guid.NewGuid(), Now, 0, settings));
        Assert.Single(team.Members);
        settings.UpdateValues(SystemSettingsValues.Defaults);
        Assert.Throws<TeamRuleException>(() => team.AddMember(Guid.NewGuid(), Now, settings.MaxMemberTeamsPerUser, settings));
        Assert.Single(team.Members);
    }
    [Fact]
    public void DeleteRequiresOnlyCaptainAndKeepsHistoricalMemberships()
    {
        var team = NewTeam(); var settings = SystemSettings.CreateDefaults(); var userId = Guid.NewGuid();
        team.AddMember(userId, Now, 0, settings);
        Assert.Throws<TeamRuleException>(() => team.Delete(Now, false)); Assert.Null(team.DeletedAt);
        team.AppointDeputy(userId, settings);
        Assert.Throws<TeamRuleException>(() => team.Delete(Now, false));
        team.Leave(userId, Now); team.Delete(Now, false);
        Assert.Equal(Now, team.DeletedAt); Assert.Equal(TeamStatus.ARCHIVED, team.Status); Assert.Equal(2, team.Members.Count);
        Assert.Throws<TeamRuleException>(() => team.AddMember(Guid.NewGuid(), Now, 0, settings));
    }
    [Fact]
    public void OpenCommitmentsBlockDeletion()
    {
        var team = NewTeam(); Assert.Throws<TeamRuleException>(() => team.Delete(Now, true)); Assert.Null(team.DeletedAt);
    }
    [Fact]
    public void InvitationCannotBeAcceptedAtExpiryOrRespondedTwice()
    {
        var settings = SystemSettings.CreateDefaults(); var team = NewTeam(); var userId = Guid.NewGuid();
        var invitation = team.Invite(team.CaptainUserId, userId, Now, settings);
        Assert.Throws<TeamRuleException>(() => invitation.Accept(invitation.ExpiresAt));
        invitation.Accept(Now); Assert.Throws<TeamRuleException>(() => invitation.Reject(Now));
        Assert.Equal(TeamRequestStatus.ACCEPTED, invitation.Status);
    }
    [Fact]
    public void ExpiredInvitationCanBeReplacedAndPendingDuplicatesAreRejected()
    {
        var settings = SystemSettings.CreateDefaults(); var team = NewTeam(); var userId = Guid.NewGuid();
        var first = team.Invite(team.CaptainUserId, userId, Now, settings);
        Assert.Throws<TeamRuleException>(() => team.Invite(team.CaptainUserId, userId, Now, settings));
        var second = team.Invite(team.CaptainUserId, userId, first.ExpiresAt, settings);
        Assert.Equal(TeamRequestStatus.EXPIRED, first.Status); Assert.Equal(TeamRequestStatus.PENDING, second.Status);
    }
    [Fact]
    public void PendingQuotaCombinesInvitationsAndJoinRequests()
    {
        var settings = SystemSettings.CreateDefaults(); settings.UpdateValues(settings.GetValues() with { MaxPendingTeamRequests = 1 });
        var team = NewTeam(); var invitation = team.Invite(team.CaptainUserId, Guid.NewGuid(), Now, settings);
        Assert.Throws<TeamRuleException>(() => team.RequestToJoin(Guid.NewGuid(), null, Now, settings));
        invitation.Cancel(Now); var request = team.RequestToJoin(Guid.NewGuid(), "  Hello  ", Now, settings);
        Assert.Equal("Hello", request.Message);
        Assert.Throws<TeamRuleException>(() => team.Invite(team.CaptainUserId, Guid.NewGuid(), Now, settings));
        request.Withdraw(Now); Assert.Throws<TeamRuleException>(() => request.Accept(team.CaptainUserId, Now));
    }
    [Fact]
    public void FailedApplicationDoesNotExpireExistingInvitationsOrMutateAggregate()
    {
        var team = NewTeam(); var settings = SystemSettings.CreateDefaults();
        var invitation = team.Invite(team.CaptainUserId, Guid.NewGuid(), Now, settings);
        var token = team.ConcurrencyToken;
        Assert.Throws<TeamRuleException>(() => team.RequestToJoin(Guid.NewGuid(), new string('x', 1001), invitation.ExpiresAt, settings));
        Assert.Equal(TeamRequestStatus.PENDING, invitation.Status); Assert.Equal(token, team.ConcurrencyToken);
        Assert.Empty(team.JoinRequests);
    }
    [Fact]
    public void TeamEventEscapesHtmlAndCarriesIndependentDeliveryState()
    {
        var teamId = Guid.NewGuid(); var history = NotificationHistory.CreateTeamEvent(Guid.NewGuid(), teamId,
            NotificationType.TEAM_CREATED, "Team", "<script>text</script>", "{\"SchemaVersion\":1}", Now, true);
        Assert.Null(history.NotificationConfigId); Assert.Equal(teamId, history.TeamId);
        Assert.Equal("<script>text</script>", history.BodyText); Assert.DoesNotContain("<script>", history.Body);
        Assert.Equal(NotificationDeliveryStatus.PENDING, history.EmailStatus);
        Assert.Equal(Now, history.ScheduledAt);
    }
}
