namespace MatchingSports.Application.Common;

public sealed class PaginatedResult<T>
{
    public PaginatedResult(
        IReadOnlyList<T> items,
        int pageNumber,
        int pageSize,
        long totalItems)
    {
        if (items is null)
            throw new ArgumentNullException(nameof(items), "Danh sách dữ liệu không được là null.");
        if (pageNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Số trang phải lớn hơn hoặc bằng 1.");
        if (pageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Kích thước trang phải lớn hơn hoặc bằng 1.");
        if (totalItems < 0)
            throw new ArgumentOutOfRangeException(nameof(totalItems), "Tổng số phần tử không được âm.");

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
