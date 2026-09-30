using MatchingSports.Application.Features.Sport;
using MatchingSports.Domain.Entities;
using MatchingSports.Infrastructure.Persistence;

namespace MatchingSports.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void DomainHasNoDependencyOnOuterLayers() =>
        AssertDoesNotReference(
            typeof(Sport).Assembly,
            "MatchingSports.Application",
            "MatchingSports.Infrastructure",
            "MatchingSports.Api");

    [Fact]
    public void ApplicationHasNoDependencyOnInfrastructureOrApi() =>
        AssertDoesNotReference(
            typeof(GetSportQuery).Assembly,
            "MatchingSports.Infrastructure",
            "MatchingSports.Api");

    [Fact]
    public void InfrastructureHasNoDependencyOnApi() =>
        AssertDoesNotReference(
            typeof(MatchingSportsDbContext).Assembly,
            "MatchingSports.Api");

    private static void AssertDoesNotReference(
        System.Reflection.Assembly assembly,
        params string[] forbiddenAssemblies)
    {
        var references = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var forbiddenAssembly in forbiddenAssemblies)
            Assert.DoesNotContain(forbiddenAssembly, references);
    }
}
