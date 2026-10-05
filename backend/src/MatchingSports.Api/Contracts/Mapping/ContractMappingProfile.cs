using AutoMapper;
using MatchingSports.Application.Features.Permission;
using MatchingSports.Application.Features.Role;
using MatchingSports.Application.Features.Sport;
using MatchingSports.Application.Features.UserProfile;

namespace MatchingSports.Api.Contracts;

public sealed class ContractMappingProfile : Profile
{
    public ContractMappingProfile()
    {
        CreateMap<GetSportsRequest, GetSportQuery>()
            .ForMember(query => query.ActorUserId, option => option.Ignore());
        CreateMap<GetRolesRequest, GetRolesQuery>()
            .ForMember(query => query.ActorUserId, option => option.Ignore());
        CreateMap<GetPermissionsRequest, GetPermissionsQuery>()
            .ForMember(query => query.ActorUserId, option => option.Ignore());
        CreateMap<GetRolePermissionMatrixRequest, GetRolePermissionMatrixQuery>()
            .ForMember(query => query.ActorUserId, option => option.Ignore());

        CreateMap<GetRolesQuery, GetRolePermissionMatrixQuery>().ReverseMap();
        CreateMap<SportFormatDto, SportFormatResponse>();
        CreateMap<SportDto, SportResponse>();
    }
}
