using AutoMapper;
using MatchingSports.Application.Features.Sport;
using MatchingSports.Application.Features.UserProfile;

namespace MatchingSports.Api.Contracts;

public sealed class ContractMappingProfile : Profile
{
    public ContractMappingProfile()
    {
        CreateMap<SportFormatDto, SportFormatResponse>();
        CreateMap<SportDto, SportResponse>();
    }
}
