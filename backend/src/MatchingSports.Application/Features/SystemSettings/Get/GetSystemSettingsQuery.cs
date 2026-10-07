using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.SystemSettings;

public sealed record GetSystemSettingsQuery(Guid ActorUserId) : IRequest<SystemSettingsDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.ReadSystemSettings];
}

public sealed class GetSystemSettingsQueryHandler(ISystemSettingsRepository repository, IMapper mapper)
    : IRequestHandler<GetSystemSettingsQuery, SystemSettingsDto>
{
    public async Task<SystemSettingsDto> Handle(GetSystemSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await repository.GetAsync(cancellationToken)
            ?? throw new NotFoundException(ErrorCodes.SystemSettingsNotFound, "Không tìm thấy cấu hình hệ thống.");
        return mapper.Map<SystemSettingsDto>(settings);
    }
}
