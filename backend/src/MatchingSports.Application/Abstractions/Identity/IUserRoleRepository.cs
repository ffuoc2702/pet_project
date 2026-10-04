using MatchingSports.Domain.Entities;

namespace MatchingSports.Application.Abstractions;

public interface IUserRoleRepository
{
    Task<UserRoleAssignment?> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken);
    Task ReplaceUserRolesAsync(UserRoleAssignment assignment, CancellationToken cancellationToken);
}
