using AutoMapper;
using MatchingSports.Application.Features.Sport.GetSport;
using MatchingSports.Domain.Entities;

namespace MatchingSports.Application.Common.Mapping;

public sealed class ApplicationMappingProfile : Profile
{
    public ApplicationMappingProfile()
    {
        CreateMap<SportFormat, SportFormatItem>();
        CreateMap<Sport, SportItem>();
    }
}
