using MatchingSports.Application.Common;
using MatchingSports.Domain.Entities;

namespace MatchingSports.Application.Abstractions;

public interface IPermissionRepository
{
    Task<PaginatedResult<Permission>> ListAsync(BaseQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Permission>> GetPermissionsAsync(CancellationToken cancellationToken);
    Task<Permission?> GetByIdAsync(Guid permissionId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Permission>> GetByCodesAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken);
}
