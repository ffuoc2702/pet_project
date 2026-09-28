using AutoMapper;
using MatchingSports.Api.Contracts.Sport;
using MatchingSports.Application.Features.Sport.GetSport;

namespace MatchingSports.Api.Contracts.Mapping;

public sealed class ContractMappingProfile : Profile
{
    public ContractMappingProfile()
    {
        CreateMap<SportFormatItem, SportFormatResponse>();
        CreateMap<SportItem, SportResponse>();
    }
}
