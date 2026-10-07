using MatchingSports.Domain.Entities;

namespace MatchingSports.Application.Abstractions;

public interface IAdminActionRepository
{
    void Add(AdminAction entry);
}
