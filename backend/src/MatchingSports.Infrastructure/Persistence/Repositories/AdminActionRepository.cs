using MatchingSports.Application.Abstractions;
using MatchingSports.Domain.Entities;

namespace MatchingSports.Infrastructure.Persistence.Repositories;

internal sealed class AdminActionRepository(MatchingSportsDbContext db) : IAdminActionRepository
{
    public void Add(AdminAction entry) => db.Set<AdminAction>().Add(entry);
}
