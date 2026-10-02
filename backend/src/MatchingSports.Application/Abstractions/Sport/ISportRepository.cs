using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Sport;
using MatchingSports.Domain.Entities;
namespace MatchingSports.Application.Abstractions;

public interface ISportRepository
{
    Task<PaginatedResult<Sport>> ListAsync(
        GetSportQuery query,
        CancellationToken cancellationToken = default);
}
