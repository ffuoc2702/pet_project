using MatchingSports.Domain.Entities;

namespace MatchingSports.Application.Abstractions;

public interface IAuthorizationAuditRepository
{
    void Add(AuthorizationAuditEntry entry);
}
