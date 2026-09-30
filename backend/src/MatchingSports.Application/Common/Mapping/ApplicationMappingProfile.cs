using AutoMapper;
using MatchingSports.Application.Features.Sport.Dtos;
using MatchingSports.Domain.Entities;

namespace MatchingSports.Application.Common.Mapping;

public sealed class ApplicationMappingProfile : Profile
{
    public ApplicationMappingProfile()
    {
        CreateMap<SportFormat, SportFormatDto>();
        CreateMap<Sport, SportDto>();
    }
}
