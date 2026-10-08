using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Team;
using MatchingSports.Domain.Entities;

namespace MatchingSports.Application.Abstractions;

public interface ITeamTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}
public interface ITeamRepository
{
    Task<ITeamTransaction> BeginWriteAsync(CancellationToken cancellationToken);
    void Add(Team team);
    void TrackNewChildren(Team team);
    Task<Team?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken, Guid? invitationId = null, Guid? joinRequestId = null);
    Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> IsActiveUserAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> IsActiveSportAsync(Guid id, CancellationToken cancellationToken);
    Task<int> CountCaptainTeamsAsync(Guid userId, CancellationToken cancellationToken);
    Task<int> CountMemberTeamsAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> HasOpenCommitmentsAsync(Guid teamId, CancellationToken cancellationToken);
    Task<PaginatedResult<Team>> ListAsync(GetTeamsQuery query, CancellationToken cancellationToken);
    Task<PaginatedResult<TeamMember>> ListMembersAsync(GetTeamMembersQuery query, CancellationToken cancellationToken);
    Task<PaginatedResult<TeamInvitation>> ListInvitationsAsync(GetTeamInvitationsQuery query, DateTimeOffset now, CancellationToken cancellationToken);
    Task<PaginatedResult<TeamJoinRequest>> ListJoinRequestsAsync(GetTeamJoinRequestsQuery query, CancellationToken cancellationToken);
}
