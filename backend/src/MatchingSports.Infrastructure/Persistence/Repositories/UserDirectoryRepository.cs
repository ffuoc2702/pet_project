using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MatchingSports.Infrastructure.Persistence.Repositories;

internal sealed class UserDirectoryRepository(MatchingSportsDbContext db) : IUserDirectoryRepository
{
    public async Task<PaginatedResult<UserListDto>> ListAsync(GetUsersQuery query, CancellationToken cancellationToken)
    {
        var users = db.Users.AsNoTracking();
        if (query.Status is { } status)
            users = users.Where(user => user.Status == status);
        if (query.FromDate is { } from)
            users = users.Where(user => user.UpdatedAt >= from);
        if (query.ToDate is { } to)
            users = users.Where(user => user.UpdatedAt <= to);
        if (!string.IsNullOrWhiteSpace(query.SearchString))
        {
            var search = query.SearchString.Trim();
            users = users.Where(user => (user.Email != null && EF.Functions.ILike(user.Email, $"%{search}%"))
                || (user.Profile != null && EF.Functions.ILike(user.Profile.DisplayName, $"%{search}%")));
        }
        var total = await users.LongCountAsync(cancellationToken);
        var items = await users.OrderBy(user => user.Email).ThenBy(user => user.Id)
            .Skip(checked((query.PageNumber - 1) * query.PageSize)).Take(query.PageSize)
            .Select(user => new UserListDto
            {
                Id = user.Id,
                DisplayName = user.Profile == null ? null : user.Profile.DisplayName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                Status = user.Status,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                CreatedBy = user.CreatedBy,
                UpdatedBy = user.UpdatedBy,
                Roles = (from membership in db.Set<IdentityUserRole<Guid>>()
                         join role in db.Roles on membership.RoleId equals role.Id
                         where membership.UserId == user.Id
                         orderby role.Name, role.Id
                         select new UserListRoleDto(role.Id, role.Name!, role.IsActive)).ToArray()
            }).ToListAsync(cancellationToken);
        return new PaginatedResult<UserListDto>(items, query.PageNumber, query.PageSize, total);
    }
}
