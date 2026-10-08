using AutoMapper;
using MatchingSports.Api.Contracts;
using MatchingSports.Application;
using MatchingSports.Application.Features.Team;
using MatchingSports.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace MatchingSports.Tests;

public sealed class TeamContractTests
{
    [Fact]
    public void TeamListMappingsPreserveFiltersAndNeverReadActorFromHttp()
    {
        var services = new ServiceCollection(); services.AddLogging(); services.AddApplication(null, typeof(ContractMappingProfile).Assembly);
        using var provider = services.BuildServiceProvider(); var mapper = provider.GetRequiredService<IMapper>();
        mapper.ConfigurationProvider.AssertConfigurationIsValid();
        var from = DateTimeOffset.UtcNow.AddDays(-1); var to = from.AddDays(1); var sportId = Guid.NewGuid();
        var teams = mapper.Map<GetTeamsQuery>(new GetTeamsRequest
        { PageNumber = 2, PageSize = 5, SearchString = "Football", FromDate = from, ToDate = to, MineOnly = true, SportId = sportId });
        Assert.Equal(Guid.Empty, teams.ActorUserId); Assert.Equal(2, teams.PageNumber); Assert.Equal(5, teams.PageSize);
        Assert.Equal("Football", teams.SearchString); Assert.Equal(from, teams.FromDate); Assert.Equal(to, teams.ToDate);
        Assert.True(teams.MineOnly); Assert.Equal(sportId, teams.SportId);
        var invitations = mapper.Map<GetTeamInvitationsQuery>(new GetTeamInvitationsRequest { Status = TeamRequestStatus.EXPIRED, PageSize = 1 });
        Assert.Null(invitations.TeamId); Assert.Equal(Guid.Empty, invitations.ActorUserId); Assert.Equal(1, invitations.PageSize);
        Assert.Equal(TeamRequestStatus.EXPIRED, invitations.Status);
        var requests = mapper.Map<GetTeamJoinRequestsQuery>(new GetTeamJoinRequestsRequest { Status = TeamRequestStatus.PENDING });
        Assert.Null(requests.TeamId); Assert.Equal(Guid.Empty, requests.ActorUserId); Assert.Equal(TeamRequestStatus.PENDING, requests.Status);
        Type[] contracts = [typeof(CreateTeamRequest), typeof(UpdateTeamRequest), typeof(TeamVersionRequest), typeof(TeamUserRequest),
            typeof(TransferTeamCaptainRequest), typeof(CreateTeamJoinRequest), typeof(GetTeamsRequest), typeof(GetTeamMembersRequest),
            typeof(GetTeamInvitationsRequest), typeof(GetTeamJoinRequestsRequest)];
        foreach (var contract in contracts)
            Assert.DoesNotContain(contract.GetProperties(), x => x.Name is "ActorUserId" or "CreatedAt" or "CreatedBy" or "UpdatedAt" or "UpdatedBy");
    }
}
