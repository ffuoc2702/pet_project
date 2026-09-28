using MediatR;

namespace MatchingSports.Application.Common.Pagination;

public interface IPaginatedQuery
{
    int PageNumber { get; }
    int PageSize { get; }
}

public abstract class PaginatedQuery<TItem> : IRequest<PaginatedResult<TItem>>, IPaginatedQuery
{
    public int PageNumber { get; init; } = PaginationDefaults.DefaultPageNumber;
    public int PageSize { get; init; } = PaginationDefaults.DefaultPageSize;
}
