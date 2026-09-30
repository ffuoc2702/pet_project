using AutoMapper;
using MatchingSports.Application.Features.Sport.Dtos;

namespace MatchingSports.Api.Contracts;

public sealed class ContractMappingProfile : Profile
{
    public ContractMappingProfile()
    {
        CreateMap<SportFormatDto, SportFormatResponse>();
        CreateMap<SportDto, SportResponse>();
    }
}
