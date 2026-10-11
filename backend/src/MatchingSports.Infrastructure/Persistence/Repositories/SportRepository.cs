using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Sport;
using MatchingSports.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MatchingSports.Infrastructure.Persistence.Repositories;

internal sealed class SportRepository(MatchingSportsDbContext db)
    : ISportRepository
{
    public void Add(Sport sport) => db.Sports.Add(sport);

    public Task<bool> HasActiveDuplicateAsync(string code, string name, Guid? excludedSportId, CancellationToken cancellationToken)
    {
        var normalizedName = name.ToLowerInvariant();
        return db.Sports.AnyAsync(sport => sport.IsActive && sport.Id != excludedSportId
            && (sport.Code == code || EF.Property<string>(sport, "NormalizedName") == normalizedName), cancellationToken);
    }

    public void Update(Sport sport)
    {
        var autoDetectChanges = db.ChangeTracker.AutoDetectChangesEnabled;
        try
        {
            // DetectChanges would classify new children with assigned IDs as updates.
            db.ChangeTracker.AutoDetectChangesEnabled = false;
            foreach (var format in sport.Formats.Where(format => db.Entry(format).State == EntityState.Detached))
                db.SportFormats.Add(format);
            // A format-only update also changes the aggregate timestamp.
            db.Entry(sport).Property(item => item.UpdatedAt).IsModified = true;
        }
        finally
        {
            db.ChangeTracker.AutoDetectChangesEnabled = autoDetectChanges;
        }
    }

    public Task<Sport?> GetForUpdateAsync(Guid sportId, CancellationToken cancellationToken) =>
        db.Sports.Include(sport => sport.Formats.OrderByDescending(format => format.UpdatedAt).ThenBy(format => format.Id))
            .SingleOrDefaultAsync(sport => sport.Id == sportId, cancellationToken);

    public Task<Sport?> GetByIdAsync(Guid sportId, CancellationToken cancellationToken) =>
        db.Sports.AsNoTracking().Include(sport => sport.Formats.OrderByDescending(format => format.UpdatedAt))
            .SingleOrDefaultAsync(sport => sport.Id == sportId, cancellationToken);
    public async Task<PaginatedResult<Sport>> ListAsync(
        GetSportsQuery query,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Sport> sports = db.Sports.AsNoTracking();

        if (query.IsActive.HasValue)
            sports = sports.Where(sport => sport.IsActive == query.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(query.SportCode))
        {
            var code = query.SportCode.Trim().ToUpperInvariant();

            sports = sports.Where(sport => sport.Code == code);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchString))
        {
            var search = query.SearchString.Trim();
            sports = sports.Where(sport =>
                EF.Functions.ILike(sport.Code, $"%{search}%")
                || EF.Functions.ILike(sport.Name, $"%{search}%"));
        }

        if (query.FromDate.HasValue)
            sports = sports.Where(sport => sport.UpdatedAt >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            sports = sports.Where(sport => sport.UpdatedAt <= query.ToDate.Value);

        var totalItems = await sports.LongCountAsync(cancellationToken);

        sports = sports.Include(sport =>
            sport.Formats.OrderByDescending(format => format.UpdatedAt));

        var offset = checked((query.PageNumber - 1) * query.PageSize);
        var items = await sports
            .OrderByDescending(sport => sport.UpdatedAt)
            .ThenBy(sport => sport.Id)
            .Skip(offset)
            .Take(query.PageSize)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return new PaginatedResult<Sport>(
            items,
            query.PageNumber,
            query.PageSize,
            totalItems);
    }
}
