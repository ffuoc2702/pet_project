using MatchingSports.Domain.Entities;

namespace MatchingSports.Application.Abstractions;

public interface ISystemSettingsRepository
{
    Task<SystemSettings?> GetAsync(CancellationToken cancellationToken);
    Task<SystemSettings?> GetForUpdateAsync(CancellationToken cancellationToken);
}
