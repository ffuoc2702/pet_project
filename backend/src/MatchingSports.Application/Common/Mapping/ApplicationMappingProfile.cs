using AutoMapper;
using MatchingSports.Application.Features.Sport;
using MatchingSports.Application.Features.UserProfile;
using MatchingSports.Domain.Entities;

namespace MatchingSports.Application.Common;

public sealed class ApplicationMappingProfile : Profile
{
    public ApplicationMappingProfile()
    {
        CreateMap<SportFormat, SportFormatDto>();
        CreateMap<Sport, SportDto>();
        CreateMap<UserProfile, UserProfileDto>();
    }
}
