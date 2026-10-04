using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Role;
using MatchingSports.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MatchingSports.Infrastructure.Persistence.Repositories;

internal sealed class RoleRepository(MatchingSportsDbContext db) : IRoleRepository
{
    public async Task<PaginatedResult<Role>> ListAsync(GetRolesQuery query, CancellationToken cancellationToken)
    {
        var roles = db.Roles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.SearchString))
        {
            var search = query.SearchString.Trim();
            roles = roles.Where(role => EF.Functions.ILike(role.Name!, $"%{search}%")
                || (role.Description != null && EF.Functions.ILike(role.Description, $"%{search}%")));
        }
        var totalItems = await roles.LongCountAsync(cancellationToken);
        var items = await roles.Include(role => role.Permissions).ThenInclude(link => link.Permission)
            .OrderBy(role => role.Name).ThenBy(role => role.Id)
            .Skip(checked((query.PageNumber - 1) * query.PageSize))
            .Take(query.PageSize)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
        return new PaginatedResult<Role>(items, query.PageNumber, query.PageSize, totalItems);
    }

    public Task<Role?> GetForUpdateAsync(Guid roleId, CancellationToken cancellationToken) =>
        db.Roles.Include(role => role.Permissions).ThenInclude(item => item.Permission)
            .SingleOrDefaultAsync(role => role.Id == roleId, cancellationToken);

    public Task<Role?> GetByIdWithPermissionsAsync(Guid roleId, CancellationToken cancellationToken) =>
        db.Roles.AsNoTracking().Include(role => role.Permissions).ThenInclude(link => link.Permission)
            .SingleOrDefaultAsync(role => role.Id == roleId, cancellationToken);

    public async Task<IReadOnlyCollection<Role>> GetByIdsAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken) =>
        await db.Roles.AsNoTracking().Where(role => roleIds.Contains(role.Id)).ToListAsync(cancellationToken);

    public Task<bool> ExistsByNormalizedNameAsync(string normalizedName, Guid? excludedRoleId, CancellationToken cancellationToken) =>
        db.Roles.AnyAsync(role => role.NormalizedName == normalizedName && role.Id != excludedRoleId, cancellationToken);

    public void Add(Role role) => db.Roles.Add(role);
}
