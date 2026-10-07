using MatchingSports.Application.Abstractions;
using MatchingSports.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MatchingSports.Infrastructure.Persistence.Repositories;

internal sealed class SystemSettingsRepository(MatchingSportsDbContext db) : ISystemSettingsRepository
{
    public Task<SystemSettings?> GetAsync(CancellationToken cancellationToken) =>
        db.SystemSettings.AsNoTracking().SingleOrDefaultAsync(settings => settings.Id == SystemSettings.SingletonId, cancellationToken);

    public Task<SystemSettings?> GetForUpdateAsync(CancellationToken cancellationToken) =>
        db.SystemSettings.SingleOrDefaultAsync(settings => settings.Id == SystemSettings.SingletonId, cancellationToken);
}
