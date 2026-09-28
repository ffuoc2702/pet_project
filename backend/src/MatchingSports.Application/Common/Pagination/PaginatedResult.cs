namespace MatchingSports.Application.Common.Pagination;

public sealed class PaginatedResult<T>
{
    public PaginatedResult(
        IReadOnlyList<T> items,
        int pageNumber,
        int pageSize,
        long totalItems)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(totalItems);

        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalItems = totalItems;
    }

    public IReadOnlyList<T> Items { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public long TotalItems { get; }
    public long TotalPages => TotalItems == 0
        ? 0
        : 1 + ((TotalItems - 1) / PageSize);
    public bool HasPreviousPage => PageNumber > 1 && TotalPages > 0;
    public bool HasNextPage => PageNumber < TotalPages;

    public PaginatedResult<TDestination> Map<TDestination>(Func<T, TDestination> map) =>
        new(
            Items.Select(map).ToArray(),
            PageNumber,
            PageSize,
            TotalItems);
}
