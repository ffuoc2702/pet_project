using System.Text.Json;
using AutoMapper;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Shared;
using MediatR;

namespace MatchingSports.Application.Features.SystemSettings;

public sealed record UpdateSystemSettingsCommand(Guid ActorUserId, Guid ConcurrencyToken, SystemSettingsInput Values)
    : IRequest<SystemSettingsDto>, IPermissionAuthorizedRequest
{
    IReadOnlyCollection<string> IPermissionAuthorizedRequest.RequiredPermissions => [PermissionCodes.UpdateSystemSettings];
}

public sealed class UpdateSystemSettingsCommandHandler(
    ISystemSettingsRepository repository, IAdminActionRepository audit, IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<UpdateSystemSettingsCommand, SystemSettingsDto>
{
    public async Task<SystemSettingsDto> Handle(UpdateSystemSettingsCommand request, CancellationToken cancellationToken)
    {
        if (request.ConcurrencyToken == Guid.Empty)
            throw new RequestValidationException(ErrorCodes.InvalidSystemSettings, "Phiên bản cấu hình là bắt buộc.",
                new Dictionary<string, string[]> { ["concurrencyToken"] = ["Vui lòng gửi phiên bản cấu hình hiện tại."] });

        var settings = await repository.GetForUpdateAsync(cancellationToken)
            ?? throw new NotFoundException(ErrorCodes.SystemSettingsNotFound, "Không tìm thấy cấu hình hệ thống.");
        if (settings.ConcurrencyToken != request.ConcurrencyToken)
            throw new ConflictException(ErrorCodes.ConcurrencyConflict, "Cấu hình đã thay đổi. Vui lòng tải lại trước khi cập nhật.");

        var before = settings.GetValues();
        try
        {
            ArgumentNullException.ThrowIfNull(request.Values);
            settings.UpdateValues(request.Values.ToDomainValues());
        }
        catch (ArgumentException exception)
        {
            throw new RequestValidationException(ErrorCodes.InvalidSystemSettings, "Cấu hình hệ thống không hợp lệ.",
                new Dictionary<string, string[]> { [exception.ParamName ?? "settings"] = [exception.GetValidationMessage()] });
        }

        audit.Add(AdminAction.Create(request.ActorUserId, AdminActionType.UPDATE_SYSTEM_SETTINGS,
            EntityTypes.SystemSettings, settings.Id, JsonSerializer.Serialize(before),
            JsonSerializer.Serialize(settings.GetValues())));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.Map<SystemSettingsDto>(settings);
    }
}
