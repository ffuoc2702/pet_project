using AutoMapper;
using MatchingSports.Api.Contracts;
using MatchingSports.Application;
using MatchingSports.Application.Features.Matching;
using MatchingSports.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace MatchingSports.Tests;

public sealed class MatchingContractTests
{
    [Fact]
    public void MappingPreservesFiltersAndKeepsActorOutOfHttpContracts()
    {
        var services = new ServiceCollection(); services.AddLogging(); services.AddApplication(null, typeof(ContractMappingProfile).Assembly);
        using var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>(); mapper.ConfigurationProvider.AssertConfigurationIsValid();
        var now = DateTimeOffset.UtcNow;
        var request = new GetListingsRequest
        {
            SportId = Guid.NewGuid(),
            FormatId = Guid.NewGuid(),
            Mode = ListingMode.FIND_PLAYERS,
            Level = SportLevel.INTERMEDIATE,
            StartFrom = now,
            StartTo = now.AddDays(1),
            Latitude = 10,
            Longitude = 106,
            RadiusMeters = 2000,
            SearchString = "football",
            PageNumber = 2,
            PageSize = 5,
            FromDate = now.AddDays(-1),
            ToDate = now,
            MineOnly = true
        };
        var query = mapper.Map<GetListingsQuery>(request);
        Assert.Equal(Guid.Empty, query.ActorUserId); Assert.Equal(request.SportId, query.SportId);
        Assert.Equal(request.FormatId, query.FormatId); Assert.Equal(request.Level, query.Level);
        Assert.Equal(request.RadiusMeters, query.RadiusMeters); Assert.Equal(request.Latitude, query.Latitude);
        Assert.Equal(request.Longitude, query.Longitude); Assert.Equal(request.StartFrom, query.StartFrom);
        Assert.Equal(request.StartTo, query.StartTo); Assert.Equal(request.FromDate, query.FromDate);
        Assert.Equal(request.ToDate, query.ToDate); Assert.Equal(2, query.PageNumber); Assert.Equal(5, query.PageSize);
        var watch = mapper.Map<ListingWatchInput>(new ListingWatchRequest
        {
            SportId = request.SportId,
            FormatId = request.FormatId,
            Latitude = 10,
            Longitude = 106,
            RadiusMeters = 2000,
            StartFrom = now,
            StartTo = now.AddDays(1)
        });
        Assert.Equal(request.SportId, watch.SportId); Assert.Equal(2000, watch.RadiusMeters);
        var input = mapper.Map<ListingInput>(new CreateListingRequest
        {
            SportId = request.SportId,
            FormatId = request.FormatId,
            Mode = ListingMode.FIND_PLAYERS,
            Title = "Kèo",
            OpenSlots = 2,
            StartAt = now.AddHours(4),
            DurationMinutes = 90,
            CutoffAt = now.AddHours(3),
            AddressText = "Sân",
            Latitude = 10,
            Longitude = 106
        });
        Assert.Equal((short)2, input.OpenSlots); Assert.Equal(ListingMode.FIND_PLAYERS, input.Mode);
        foreach (var type in new[] { typeof(CreateListingRequest), typeof(UpdateListingRequest), typeof(JoinListingRequest),
            typeof(ListingWatchRequest), typeof(UpdateListingWatchRequest), typeof(GetListingsRequest), typeof(GetListingWatchesRequest) })
            Assert.DoesNotContain(type.GetProperties(), property => property.Name is "ActorUserId" or "UserId" or "CreatedAt" or "CreatedBy" or "UpdatedAt" or "UpdatedBy");
    }
}
