using MediatR;

namespace MatchingSports.Application.Common;

public interface IDateRangeQuery
{
    DateTimeOffset? FromDate { get; }
    DateTimeOffset? ToDate { get; }
}

public interface IBaseQuery : IDateRangeQuery
{
    string? SearchString { get; }
}

public abstract record BaseQuery : PaginatedQuery, IBaseQuery
{
    public string? SearchString { get; init; }
    public DateTimeOffset? FromDate { get; init; }
    public DateTimeOffset? ToDate { get; init; }
}

public abstract record BaseQuery<TItem> : BaseQuery, IRequest<PaginatedResult<TItem>>;
