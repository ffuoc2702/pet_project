using AutoMapper;
using MatchingSports.Application.Features.Role;
using MatchingSports.Application.Features.Sport;
using MatchingSports.Application.Features.UserProfile;
using MatchingSports.Application.Features.UserSportProfile;
using MatchingSports.Domain.Entities;

namespace MatchingSports.Application.Common;

public sealed class ApplicationMappingProfile : Profile
{
    public ApplicationMappingProfile()
    {
        CreateMap<SportFormat, SportFormatDto>();
        CreateMap<Sport, SportDto>();
        CreateMap<UserProfile, UserProfileDto>();
        CreateMap<UserSportProfile, UserSportProfileDto>()
            .ForMember(dto => dto.PreferredFormats, options => options.MapFrom(profile => profile.PreferredFormats.OrderBy(format => format.Code)));
        CreateMap<Permission, PermissionDto>();
        CreateMap<Role, RoleDto>()
            .ForCtorParam(nameof(RoleDto.Permissions), options => options.MapFrom(role =>
                role.Permissions.OrderBy(item => item.Permission.Code).Select(item => item.Permission)));
    }
}
