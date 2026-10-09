using MatchingSports.Application;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Matching;
using MatchingSports.Application.Features.PublicProfile;
using MatchingSports.Domain.Entities;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace MatchingSports.Tests;

public sealed class MatchingAuthorizationTests
{
    [Fact]
    public async Task EveryMatchingUseCaseRejectsMissingPermissionsBeforeResolvingPersistence()
    {
        var actor = Guid.NewGuid(); var id = Guid.NewGuid(); var token = Guid.NewGuid();
        var requests = new object[]
        {
            new GetPublicUserProfileQuery(actor, id), new GetPublicTeamProfileQuery(actor, id),
            new CreateListingCommand(actor, null!), new UpdateListingCommand(actor, id, token, null!),
            new CancelListingCommand(actor, id, token, "Reason"), new GetListingsQuery { ActorUserId = actor }, new GetListingQuery(actor, id),
            new CreateListingRequestCommand(actor, id, null, null), new AcceptListingRequestCommand(actor, id, id, token),
            new RejectListingRequestCommand(actor, id, id, token, "Reason"), new WithdrawListingRequestCommand(actor, id, id, token),
            new GetListingRequestsQuery { ActorUserId = actor }, new GetListingRequestQuery(actor, id, id),
            new ConfirmMatchCommand(actor, id, token), new CancelMatchCommand(actor, id, token, "Reason"),
            new CancelAnyMatchCommand(actor, id, token, "Reason"), new CompleteMatchCommand(actor, id, token),
            new GetMatchesQuery { ActorUserId = actor }, new GetMatchQuery(actor, id), new GetMatchContactsQuery(actor, id),
            new CreateListingWatchCommand(actor, null!, false), new UpdateListingWatchCommand(actor, id, token, null!, false),
            new PauseListingWatchCommand(actor, id, token), new ActivateListingWatchCommand(actor, id, token),
            new CancelListingWatchCommand(actor, id, token), new GetListingWatchesQuery { ActorUserId = actor }, new GetListingWatchQuery(actor, id)
        };
        var services = new ServiceCollection(); services.AddLogging(); services.AddApplication();
        services.AddSingleton<IUserRoleRepository>(new DeniedRepository());
        using var provider = services.BuildServiceProvider(); var mediator = provider.GetRequiredService<IMediator>();
        foreach (var request in requests)
        {
            Assert.NotEmpty(((IPermissionAuthorizedRequest)request).RequiredPermissions);
            await Assert.ThrowsAsync<ForbiddenException>(() => mediator.Send(request));
        }
    }
    private sealed class DeniedRepository : IUserRoleRepository
    {
        public Task<UserRoleAssignment?> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult<UserRoleAssignment?>(new(userId, [], []));
        public Task ReplaceUserRolesAsync(UserRoleAssignment assignment, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
