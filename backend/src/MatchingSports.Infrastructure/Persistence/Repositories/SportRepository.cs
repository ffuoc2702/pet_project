using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common.Pagination;
using MatchingSports.Application.Features.Sport.GetSport;
using MatchingSports.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MatchingSports.Infrastructure.Persistence.Repositories;

internal sealed class SportRepository(MatchingSportsDbContext db)
    : ISportRepository
{
    public async Task<PaginatedResult<Sport>> ListAsync(
        GetSportQuery query,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Sport> sports = db.Sports.AsNoTracking();

        if (query.ActiveOnly)
        {
            sports = sports.Where(sport => sport.IsActive);
        }

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

        sports = query.ActiveOnly
            ? sports.Include(sport =>
                sport.Formats
                    .Where(format => format.IsActive)
                    .OrderByDescending(format => format.UpdatedAt))
            : sports.Include(sport =>
                sport.Formats
                    .OrderByDescending(format => format.UpdatedAt));

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
