using System.ComponentModel.DataAnnotations;
using MatchingSports.Application.Common;

namespace MatchingSports.Api.Contracts;

public abstract class PaginatedRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Số trang phải lớn hơn hoặc bằng 1.")]
    public int PageNumber { get; init; } = PaginationDefaults.DefaultPageNumber;

    [Range(1, PaginationDefaults.MaxPageSize,
        ErrorMessage = "Kích thước trang phải từ 1 đến 100.")]
    public int PageSize { get; init; } = PaginationDefaults.DefaultPageSize;
}
