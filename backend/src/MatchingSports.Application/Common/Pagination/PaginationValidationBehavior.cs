using MatchingSports.Application.Common.Exceptions;
using MediatR;

namespace MatchingSports.Application.Common.Pagination;

public sealed class PaginationValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IPaginatedQuery paginatedQuery)
            return await next(cancellationToken);

        var errors = new Dictionary<string, string[]>();

        if (paginatedQuery.PageNumber < 1)
            errors["pageNumber"] = ["Số trang phải lớn hơn hoặc bằng 1."];

        if (paginatedQuery.PageSize is < 1 or > PaginationDefaults.MaxPageSize)
        {
            errors["pageSize"] =
            [$"Kích thước trang phải từ 1 đến {PaginationDefaults.MaxPageSize}."];
        }

        if (paginatedQuery.PageNumber >= 1
            && paginatedQuery.PageSize >= 1
            && ((long)paginatedQuery.PageNumber - 1) * paginatedQuery.PageSize > int.MaxValue)
        {
            errors["pageNumber"] = ["Vị trí trang vượt quá giới hạn hỗ trợ."];
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(
                "INVALID_PAGINATION",
                "Thông tin phân trang không hợp lệ.",
                errors);
        }

        return await next(cancellationToken);
    }
}
