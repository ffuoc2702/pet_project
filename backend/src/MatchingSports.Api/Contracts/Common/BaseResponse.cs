namespace MatchingSports.Api.Contracts.Common;

public class BaseResponse<T>
{
    public required T Data { get; init; }
    public string? Message { get; init; }
    public required string TraceId { get; init; }
    public required string CorrelationId { get; init; }
}

public sealed class PaginatedResponse<T> : BaseResponse<IReadOnlyList<T>>
{
    public required PaginationMetadata Pagination { get; init; }
}

public sealed class PaginationMetadata
{
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public required long TotalItems { get; init; }
    public required long TotalPages { get; init; }
    public required bool HasPreviousPage { get; init; }
    public required bool HasNextPage { get; init; }
}
