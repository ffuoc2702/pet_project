
namespace MatchingSports.Application.Common;

public interface IDateRangeQuery
{
    DateTimeOffset? FromDate { get; }
    DateTimeOffset? ToDate { get; }
}

public abstract class BaseQuery<TItem> : PaginatedQuery<TItem>, IDateRangeQuery
{
    public string? SearchString { get; init; }
    public DateTimeOffset? FromDate { get; init; }
    public DateTimeOffset? ToDate { get; init; }
}
