using MatchingSports.Application.Abstractions;

namespace MatchingSports.Infrastructure.Persistence;

internal sealed class UnitOfWork(MatchingSportsDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
