using MatchingSports.Application.Common;

namespace MatchingSports.Tests;

public sealed class PaginationTests
{
    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(long.MaxValue, 100, 92233720368547759)]
    public void TotalPagesUsesOverflowSafeIntegerMath(
        long totalItems,
        int pageSize,
        long expectedPages)
    {
        var result = new PaginatedResult<int>([], 1, pageSize, totalItems);

        Assert.Equal(expectedPages, result.TotalPages);
    }

    [Fact]
    public void MapPreservesPaginationMetadata()
    {
        var source = new PaginatedResult<int>([1, 2], 2, 2, 5);

        var destination = source.Map(value => value.ToString(System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal(["1", "2"], destination.Items);
        Assert.Equal(2, destination.PageNumber);
        Assert.Equal(2, destination.PageSize);
        Assert.Equal(5, destination.TotalItems);
        Assert.True(destination.HasPreviousPage);
        Assert.True(destination.HasNextPage);
    }
}
