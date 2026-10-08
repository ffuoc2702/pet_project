using MatchingSports.Application.Abstractions;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MatchingSports.Infrastructure.Persistence.Repositories;

internal sealed class UserRoleRepository(MatchingSportsDbContext db) : IUserRoleRepository
{
    public async Task<UserRoleAssignment?> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == userId && user.Status == UserStatus.ACTIVE, cancellationToken);
        if (user is null)
            return null;

        var memberships = await (from membership in db.Set<IdentityUserRole<Guid>>()
                                 join role in db.Roles on membership.RoleId equals role.Id
                                 where membership.UserId == userId
                                 orderby role.Name
                                 select new
                                 {
                                     role.Id,
                                     role.Name,
                                     role.IsActive,
                                     Permissions = role.Permissions.Select(link => link.Permission.Code).ToArray()
                                 }).ToArrayAsync(cancellationToken);
        var activeRoles = memberships.Where(role => role.IsActive).ToArray();
        var roles = activeRoles.Select(role => role.Name!).ToArray();
        var permissions = activeRoles.SelectMany(role => role.Permissions).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var roleIds = memberships.Select(role => role.Id).Order().ToArray();
        return new UserRoleAssignment(userId, roles, permissions, user.ConcurrencyStamp, roleIds);
    }

    public async Task ReplaceUserRolesAsync(UserRoleAssignment assignment, CancellationToken cancellationToken)
    {
        var memberships = await db.Set<IdentityUserRole<Guid>>()
            .Where(item => item.UserId == assignment.UserId).ToListAsync(cancellationToken);
        var roleIds = assignment.RoleIds;
        var user = await db.Users.SingleAsync(user => user.Id == assignment.UserId, cancellationToken);
        db.Entry(user).Property(item => item.ConcurrencyStamp).OriginalValue = assignment.OriginalConcurrencyStamp;
        user.ConcurrencyStamp = assignment.ConcurrencyStamp;

        db.RemoveRange(memberships.Where(item => !roleIds.Contains(item.RoleId)));
        foreach (var roleId in roleIds.Where(id => memberships.All(item => item.RoleId != id)))
            db.Add(new IdentityUserRole<Guid> { UserId = assignment.UserId, RoleId = roleId });
    }
}
