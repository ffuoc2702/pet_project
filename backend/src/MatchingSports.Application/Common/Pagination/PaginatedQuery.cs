using MediatR;

namespace MatchingSports.Application.Common;

public interface IPaginatedQuery
{
    int PageNumber { get; }
    int PageSize { get; }
}

public abstract record PaginatedQuery : IPaginatedQuery
{
    public int PageNumber { get; init; } = PaginationDefaults.DefaultPageNumber;
    public int PageSize { get; init; } = PaginationDefaults.DefaultPageSize;
}

public abstract record PaginatedQuery<TItem> : PaginatedQuery, IRequest<PaginatedResult<TItem>>;
