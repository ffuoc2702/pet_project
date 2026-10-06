using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Sport;
using MatchingSports.Domain.Entities;
namespace MatchingSports.Application.Abstractions;

public interface ISportRepository
{
    void Add(Sport sport);
    void Update(Sport sport);
    Task<bool> HasActiveDuplicateAsync(string code, string name, Guid? excludedSportId, CancellationToken cancellationToken);
    Task<Sport?> GetForUpdateAsync(Guid sportId, CancellationToken cancellationToken);
    Task<Sport?> GetByIdAsync(Guid sportId, CancellationToken cancellationToken);
    Task<PaginatedResult<Sport>> ListAsync(
        GetSportsQuery query,
        CancellationToken cancellationToken = default);
}
