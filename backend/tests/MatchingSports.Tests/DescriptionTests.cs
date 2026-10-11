using System.ComponentModel.DataAnnotations;
using MatchingSports.Api.Contracts;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.ValueObjects;
using MatchingSports.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MatchingSports.Tests;

public sealed class DescriptionTests
{
    [Fact]
    public void DescriptionAccepts200CharactersAndRejects201AcrossDomainResources()
    {
        var maximum = new string('x', 200);
        var oversized = maximum + "x";
        Action<string>[] assignDescriptions =
        [
            value => ResourceDescription.Normalize(value),
            value => Sport.Create("TEST", "Test", [SportFormatDetails.Create("ONE", "One", 1)], value),
            value => SportFormatDetails.Create("ONE", "One", 1, description: value),
            value => Role.Create("TEST", value),
            value => Permission.Create("test.read", "Test", value),
            value => { _ = new Report { Description = value }; }
        ];
        foreach (var assign in assignDescriptions)
        {
            assign(maximum);
            Assert.ThrowsAny<ArgumentException>(() => assign(oversized));
        }
    }

    [Fact]
    public void OversizedTeamDescriptionDoesNotPartiallyChangeDetails()
    {
        var team = new Team();
        team.UpdateDetails("Original", new string('x', 200), null, null);
        var token = team.ConcurrencyToken;
        Assert.Throws<TeamRuleException>(() => team.UpdateDetails("Changed", new string('x', 201), null, null));
        Assert.Equal("Original", team.Name);
        Assert.Equal(200, team.Description!.Length);
        Assert.Equal(token, team.ConcurrencyToken);
    }

    [Fact]
    public void AllDescriptionHttpContractsEnforceTheSameBoundary()
    {
        var contracts = typeof(CreateSportRequest).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(CreateSportRequest).Namespace && !type.IsAbstract)
            .Where(type => type.GetProperty("Description") is not null).ToArray();
        Assert.NotEmpty(contracts);
        foreach (var contract in contracts)
        {
            var instance = Activator.CreateInstance(contract)!;
            var context = new ValidationContext(instance) { MemberName = "Description" };
            Assert.True(Validator.TryValidateProperty(new string('x', 200), context, []));
            Assert.False(Validator.TryValidateProperty(new string('x', 201), context, []));
        }
    }

    [Fact]
    public void EveryPersistedDescriptionHasTheSameMaximumLength()
    {
        var options = new DbContextOptionsBuilder<MatchingSportsDbContext>()
            .UseNpgsql("Host=localhost;Database=model_test;Username=postgres", npgsql => npgsql.UseNetTopologySuite()).Options;
        using var db = new MatchingSportsDbContext(options);
        var descriptions = db.Model.GetEntityTypes().SelectMany(type => type.GetProperties())
            .Where(property => property.Name == "Description").ToArray();
        Assert.Equal(8, descriptions.Length);
        Assert.All(descriptions, property => Assert.Equal(200, property.GetMaxLength()));
    }
}
