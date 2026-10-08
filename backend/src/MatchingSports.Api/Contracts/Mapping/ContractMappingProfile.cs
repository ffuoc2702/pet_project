using AutoMapper;
using MatchingSports.Application.Features.Permission;
using MatchingSports.Application.Features.Role;
using MatchingSports.Application.Features.Sport;
using MatchingSports.Application.Features.UserProfile;
using MatchingSports.Application.Features.Notification;

namespace MatchingSports.Api.Contracts;

public sealed class ContractMappingProfile : Profile
{
    public ContractMappingProfile()
    {
        CreateMap<SportFormatRequest, SportFormatInput>();
        CreateMap<NotificationConfigRequest, NotificationInput>()
            .ForCtorParam("Metadata", options => options.MapFrom(x => x.Metadata.GetRawText()));
        CreateMap<UpdateNotificationConfigRequest, NotificationInput>()
            .ForCtorParam("Metadata", options => options.MapFrom(x => x.Metadata.GetRawText()));
        CreateMap<GetNotificationConfigsRequest, GetNotificationConfigsQuery>()
            .ForMember(x => x.ActorUserId, options => options.Ignore());
        CreateMap<GetNotificationHistoryRequest, GetNotificationHistoryQuery>()
            .ForMember(x => x.ActorUserId, options => options.Ignore())
            .ForMember(x => x.VisibleAt, options => options.Ignore());
        CreateMap<GetNotificationHistoryRequest, GetNotificationsQuery>()
            .ForMember(x => x.ActorUserId, options => options.Ignore())
            .ForMember(x => x.VisibleAt, options => options.Ignore());
        CreateMap<GetSportsRequest, GetSportsQuery>()
            .ForMember(query => query.ActorUserId, option => option.Ignore());
        CreateMap<GetRolesRequest, GetRolesQuery>()
            .ForMember(query => query.ActorUserId, option => option.Ignore());
        CreateMap<GetPermissionsRequest, GetPermissionsQuery>()
            .ForMember(query => query.ActorUserId, option => option.Ignore());
        CreateMap<GetRolePermissionMatrixRequest, GetRolePermissionMatrixQuery>()
            .ForMember(query => query.ActorUserId, option => option.Ignore());

    }
}
