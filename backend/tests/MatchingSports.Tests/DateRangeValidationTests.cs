using MatchingSports.Application.Common;
using MediatR;
using MatchingSports.Shared;

namespace MatchingSports.Tests;

public sealed class DateRangeValidationTests
{
    [Fact]
    public async Task InvalidDateRangeThrowsRequestValidationException()
    {
        var behavior = new DateRangeValidationBehavior<TestQuery, Unit>();
        var query = new TestQuery
        {
            FromDate = new DateTimeOffset(2026, 9, 29, 0, 0, 0, TimeSpan.Zero),
            ToDate = new DateTimeOffset(2026, 9, 28, 0, 0, 0, TimeSpan.Zero)
        };

        var exception = await Assert.ThrowsAsync<RequestValidationException>(() =>
            behavior.Handle(
                query,
                _ => Task.FromResult(Unit.Value),
                CancellationToken.None));

        Assert.Equal(ErrorCodes.InvalidDateRange, exception.Code);
        Assert.Contains("fromDate", exception.Errors.Keys);
    }

    [Fact]
    public async Task ValidDateRangeContinuesPipeline()
    {
        var behavior = new DateRangeValidationBehavior<TestQuery, Unit>();
        var query = new TestQuery
        {
            FromDate = new DateTimeOffset(2026, 9, 28, 0, 0, 0, TimeSpan.Zero),
            ToDate = new DateTimeOffset(2026, 9, 29, 0, 0, 0, TimeSpan.Zero)
        };

        var result = await behavior.Handle(
            query,
            _ => Task.FromResult(Unit.Value),
            CancellationToken.None);

        Assert.Equal(Unit.Value, result);
    }

    private sealed class TestQuery : BaseQuery<Unit>;
}
