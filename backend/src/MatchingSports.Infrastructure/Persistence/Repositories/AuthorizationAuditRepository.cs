using MatchingSports.Application.Abstractions;
using MatchingSports.Domain.Entities;

namespace MatchingSports.Infrastructure.Persistence.Repositories;

internal sealed class AuthorizationAuditRepository(MatchingSportsDbContext db) : IAuthorizationAuditRepository
{
    public void Add(AuthorizationAuditEntry entry) => db.Set<AuthorizationAuditEntry>().Add(entry);
}
