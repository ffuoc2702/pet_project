using MatchingSports.Application.Abstractions;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.ValueObjects;
using MatchingSports.Infrastructure;
using MatchingSports.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MatchingSports.Tests;

public sealed class SportPersistenceTests
{
    [Fact]
    public void FormatOnlyUpdateTracksNewChildrenAsInsertsAndRetainsExistingReferences()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:matchingsports"] = "Host=localhost;Database=tracking_validation;Username=postgres"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MatchingSportsDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<ISportRepository>();
        var sport = Sport.Create("TEST", "Test sport",
            [SportFormatDetails.Create("SINGLES", "Singles", 1), SportFormatDetails.Create("OLD", "Old", 2)]);
        db.Attach(sport);
        var existing = sport.Formats.Single(format => format.Code == "SINGLES");
        var omitted = sport.Formats.Single(format => format.Code == "OLD");
        sport.UpdateDetails("TEST", "Test sport",
            [SportFormatDetails.Create("SINGLES", "Updated singles", 1, existing.Id), SportFormatDetails.Create("TEAM", "Team", 5)]);

        repository.Update(sport);
        db.ChangeTracker.DetectChanges();

        var added = sport.Formats.Single(format => format.Code == "TEAM");
        Assert.Equal(EntityState.Added, db.Entry(added).State);
        Assert.Equal(EntityState.Modified, db.Entry(existing).State);
        Assert.Equal(EntityState.Unchanged, db.Entry(omitted).State);
        Assert.True(omitted.IsActive);
        Assert.True(db.Entry(sport).Property(item => item.UpdatedAt).IsModified);
        Assert.Same(sport, added.Sport);
        Assert.Equal(sport.Id, added.SportId);
        Assert.DoesNotContain(db.ChangeTracker.Entries(), entry => entry.State == EntityState.Deleted);
    }
}
