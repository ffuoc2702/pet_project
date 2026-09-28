using MatchingSports.Domain.Entities;
using MatchingSports.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MatchingSports.Tests;

public sealed class ConcurrencyModelTests
{
    [Theory]
    [InlineData(typeof(Listing))]
    [InlineData(typeof(ListingRequest))]
    [InlineData(typeof(Match))]
    public void WritableAggregateUsesGuidConcurrencyToken(Type entityType)
    {
        var options = new DbContextOptionsBuilder<MatchingSportsDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=model_validation;Username=postgres;Password=postgres",
                npgsql => npgsql.UseNetTopologySuite())
            .Options;
        using var dbContext = new MatchingSportsDbContext(options);

        var modelEntity = dbContext.Model.FindEntityType(entityType);
        var token = modelEntity?.FindProperty(nameof(IHasConcurrencyToken.ConcurrencyToken));

        Assert.NotNull(token);
        Assert.Equal(typeof(Guid), token.ClrType);
        Assert.True(token.IsConcurrencyToken);
        Assert.Null(modelEntity?.FindProperty("Version"));
    }
}
