using AutoMapper;
using MatchingSports.Application.Features.Notification;
using MatchingSports.Application.Features.Role;
using MatchingSports.Application.Features.Sport;
using MatchingSports.Application.Features.SystemSettings;
using MatchingSports.Application.Features.UserProfile;
using MatchingSports.Application.Features.UserSportProfile;
using MatchingSports.Application.Features.Team;
using MatchingSports.Domain.Entities;

namespace MatchingSports.Application.Common;

public sealed class ApplicationMappingProfile : Profile
{
    public ApplicationMappingProfile()
    {
        CreateMap<Team, TeamDto>();
        CreateMap<TeamMember, TeamMemberDto>();
        CreateMap<TeamInvitation, TeamInvitationDto>();
        CreateMap<TeamJoinRequest, TeamJoinRequestDto>();
        CreateMap<SportFormat, SportFormatDto>();
        CreateMap<Sport, SportDto>();
        CreateMap<UserProfile, UserProfileDto>();
        CreateMap<SystemSettings, SystemSettingsDto>();
        CreateMap<UserSportProfile, UserSportProfileDto>()
            .ForMember(dto => dto.PreferredFormats, options => options.MapFrom(profile => profile.PreferredFormats.OrderBy(format => format.Code)));
        CreateMap<Permission, PermissionDto>();
        CreateMap<NotificationConfig, NotificationConfigDto>()
            .ForMember(x => x.Metadata, options => options.MapFrom(x => ParseMetadata(x.Metadata)))
            .ForMember(x => x.UserIds, options => options.MapFrom(x => x.Users.Select(target => target.UserId)))
            .ForMember(x => x.TeamIds, options => options.MapFrom(x => x.Teams.Select(target => target.TeamId)));
        CreateMap<NotificationHistory, NotificationHistoryDto>()
            .ForMember(x => x.Metadata, options => options.MapFrom(x => ParseMetadata(x.PayloadJson)));
        CreateMap<GetRolesQuery, GetRolePermissionMatrixQuery>().ReverseMap();
        CreateMap<Role, RoleDto>()
            .ForCtorParam(nameof(RoleDto.Permissions), options => options.MapFrom(role =>
                role.Permissions.OrderBy(item => item.Permission.Code).Select(item => item.Permission)));
    }

    private static System.Text.Json.JsonElement ParseMetadata(string json) =>
        System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
}
