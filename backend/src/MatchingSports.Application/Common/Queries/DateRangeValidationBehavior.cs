using MatchingSports.Application.Common.Exceptions;
using MediatR;

namespace MatchingSports.Application.Common.Queries;

public sealed class DateRangeValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is IDateRangeQuery dateRange
            && dateRange.FromDate.HasValue
            && dateRange.ToDate.HasValue
            && dateRange.FromDate > dateRange.ToDate)
        {
            throw new RequestValidationException(
                "INVALID_DATE_RANGE",
                "Khoảng thời gian không hợp lệ.",
                new Dictionary<string, string[]>
                {
                    ["fromDate"] = ["FromDate phải nhỏ hơn hoặc bằng ToDate."]
                });
        }

        return next(cancellationToken);
    }
}
