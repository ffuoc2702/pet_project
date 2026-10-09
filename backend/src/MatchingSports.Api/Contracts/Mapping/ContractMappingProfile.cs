using AutoMapper;
using MatchingSports.Application.Features.Matching;
using MatchingSports.Application.Features.Notification;
using MatchingSports.Application.Features.Permission;
using MatchingSports.Application.Features.Role;
using MatchingSports.Application.Features.Sport;
using MatchingSports.Application.Features.Team;
using MatchingSports.Application.Features.UserProfile;

namespace MatchingSports.Api.Contracts;

public sealed class ContractMappingProfile : Profile
{
    public ContractMappingProfile()
    {
        CreateMap<CreateListingRequest, ListingInput>();
        CreateMap<UpdateListingRequest, ListingInput>();
        CreateMap<ListingWatchRequest, ListingWatchInput>();
        CreateMap<UpdateListingWatchRequest, ListingWatchInput>();
        CreateMap<GetListingsRequest, GetListingsQuery>().ForMember(query => query.ActorUserId, options => options.Ignore());
        CreateMap<GetListingRequestsRequest, GetListingRequestsQuery>().ForMember(query => query.ActorUserId, options => options.Ignore())
            .ForMember(query => query.ListingId, options => options.Ignore());
        CreateMap<GetMatchesRequest, GetMatchesQuery>().ForMember(query => query.ActorUserId, options => options.Ignore());
        CreateMap<GetListingWatchesRequest, GetListingWatchesQuery>().ForMember(query => query.ActorUserId, options => options.Ignore());
        CreateMap<GetTeamsRequest, GetTeamsQuery>().ForMember(x => x.ActorUserId, options => options.Ignore());
        CreateMap<GetTeamMembersRequest, GetTeamMembersQuery>()
            .ForMember(x => x.ActorUserId, options => options.Ignore()).ForMember(x => x.TeamId, options => options.Ignore());
        CreateMap<GetTeamInvitationsRequest, GetTeamInvitationsQuery>()
            .ForMember(x => x.ActorUserId, options => options.Ignore()).ForMember(x => x.TeamId, options => options.Ignore());
        CreateMap<GetTeamJoinRequestsRequest, GetTeamJoinRequestsQuery>()
            .ForMember(x => x.ActorUserId, options => options.Ignore()).ForMember(x => x.TeamId, options => options.Ignore());
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
