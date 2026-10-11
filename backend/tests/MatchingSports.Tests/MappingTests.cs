using AutoMapper;
using MatchingSports.Api.Contracts;
using MatchingSports.Application;
using MatchingSports.Application.Common;
using MatchingSports.Application.Features.Permission;
using MatchingSports.Application.Features.Role;
using MatchingSports.Application.Features.Sport;
using Microsoft.Extensions.DependencyInjection;

namespace MatchingSports.Tests;

public sealed class MappingTests
{
    [Theory]
    [InlineData(typeof(GetSportsRequest), typeof(GetSportsQuery))]
    [InlineData(typeof(GetRolesRequest), typeof(GetRolesQuery))]
    [InlineData(typeof(GetPermissionsRequest), typeof(GetPermissionsQuery))]
    [InlineData(typeof(GetRolePermissionMatrixRequest), typeof(GetRolePermissionMatrixQuery))]
    public void ListRequestMappingPreservesInheritedFiltersAndDoesNotSupplyActor(Type requestType, Type queryType)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication(null, typeof(ContractMappingProfile).Assembly);
        using var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();
        var fromDate = DateTimeOffset.Parse("2026-10-01T00:00:00+00:00", System.Globalization.CultureInfo.InvariantCulture);
        var request = (BaseRequest)Activator.CreateInstance(requestType)!;
        // Simulate values supplied by HTTP model binding, including inherited init properties.
        requestType.GetProperty(nameof(BaseRequest.PageNumber))!.SetValue(request, 3);
        requestType.GetProperty(nameof(BaseRequest.PageSize))!.SetValue(request, 7);
        requestType.GetProperty(nameof(BaseRequest.SearchString))!.SetValue(request, "  organizer  ");
        requestType.GetProperty(nameof(BaseRequest.FromDate))!.SetValue(request, fromDate);
        requestType.GetProperty(nameof(BaseRequest.ToDate))!.SetValue(request, fromDate.AddDays(1));
        var query = (BaseQuery)mapper.Map(request, requestType, queryType);
        Assert.Equal(3, query.PageNumber);
        Assert.Equal(7, query.PageSize);
        Assert.Equal(request.SearchString, query.SearchString);
        Assert.Equal(request.FromDate, query.FromDate);
        Assert.Equal(request.ToDate, query.ToDate);
        Assert.Equal(Guid.Empty, ((IPermissionAuthorizedRequest)query).ActorUserId);

        var defaults = (BaseQuery)mapper.Map(Activator.CreateInstance(requestType)!, requestType, queryType);
        Assert.Equal(PaginationDefaults.DefaultPageNumber, defaults.PageNumber);
        Assert.Equal(PaginationDefaults.DefaultPageSize, defaults.PageSize);
        Assert.Null(defaults.SearchString);
        if (query is GetSportsQuery sport)
        {
            Assert.Null(sport.IsActive);
            var filters = mapper.Map<GetSportsQuery>(new GetSportsRequest { IsActive = false, SportCode = "FOOTBALL" });
            Assert.False(filters.IsActive);
            Assert.Equal("FOOTBALL", filters.SportCode);
        }
    }

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
