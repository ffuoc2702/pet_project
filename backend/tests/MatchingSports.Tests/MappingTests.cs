using AutoMapper;
using MatchingSports.Api.Contracts;
using MatchingSports.Application;
using Microsoft.Extensions.DependencyInjection;

namespace MatchingSports.Tests;

public sealed class MappingTests
{
    [Fact]
    public void AutoMapperConfigurationIsValid()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication(null, typeof(ContractMappingProfile).Assembly);

        using var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }
}
