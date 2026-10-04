using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Role;
using MatchingSports.Domain.Entities;

namespace MatchingSports.Application.Abstractions;

public interface IRoleRepository
{
    Task<PaginatedResult<Role>> ListAsync(GetRolesQuery query, CancellationToken cancellationToken);
    Task<Role?> GetForUpdateAsync(Guid roleId, CancellationToken cancellationToken);
    Task<Role?> GetByIdWithPermissionsAsync(Guid roleId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Role>> GetByIdsAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken);
    Task<bool> ExistsByNormalizedNameAsync(string normalizedName, Guid? excludedRoleId, CancellationToken cancellationToken);
    void Add(Role role);
}
