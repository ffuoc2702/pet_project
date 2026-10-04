using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MatchingSports.Infrastructure.Persistence.Repositories;

internal sealed class PermissionRepository(MatchingSportsDbContext db) : IPermissionRepository
{
    public async Task<PaginatedResult<Permission>> ListAsync(BaseQuery query, CancellationToken cancellationToken)
    {
        var permissions = db.Permissions.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.SearchString))
        {
            var search = query.SearchString.Trim();
            permissions = permissions.Where(permission => EF.Functions.ILike(permission.Code, $"%{search}%")
                || EF.Functions.ILike(permission.Name, $"%{search}%"));
        }
        if (query.FromDate.HasValue)
            permissions = permissions.Where(permission => permission.UpdatedAt >= query.FromDate.Value);
        if (query.ToDate.HasValue)
            permissions = permissions.Where(permission => permission.UpdatedAt <= query.ToDate.Value);
        var totalItems = await permissions.LongCountAsync(cancellationToken);
        var items = await permissions.OrderBy(permission => permission.Code).ThenBy(permission => permission.Id)
            .Skip(checked((query.PageNumber - 1) * query.PageSize)).Take(query.PageSize).ToArrayAsync(cancellationToken);
        return new PaginatedResult<Permission>(items, query.PageNumber, query.PageSize, totalItems);
    }

    public Task<Permission?> GetByIdAsync(Guid permissionId, CancellationToken cancellationToken) =>
        db.Permissions.AsNoTracking().SingleOrDefaultAsync(permission => permission.Id == permissionId, cancellationToken);
    public async Task<IReadOnlyCollection<Permission>> GetPermissionsAsync(CancellationToken cancellationToken) =>
        await db.Permissions.AsNoTracking().OrderBy(permission => permission.Code).ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Permission>> GetByCodesAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken) =>
        await db.Permissions.Where(permission => codes.Contains(permission.Code)).ToArrayAsync(cancellationToken);
}
