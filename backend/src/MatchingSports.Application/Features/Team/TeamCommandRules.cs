using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Entities;
using MatchingSports.Shared;

namespace MatchingSports.Application.Features.Team;

internal static class TeamCommandRules
{
    public static void RequireCaptain(Domain.Entities.Team team, Guid actorId)
    {
        if (!team.IsCaptain(actorId)) throw new ForbiddenException(ErrorCodes.Forbidden, "Chỉ đội trưởng được thực hiện thao tác này.");
    }
    public static void RequireManager(Domain.Entities.Team team, Guid actorId)
    {
        if (!team.CanManageMembers(actorId)) throw new ForbiddenException(ErrorCodes.Forbidden, "Chỉ đội trưởng hoặc đội phó được thực hiện thao tác này.");
    }
    public static void RequireOwner(Guid userId, Guid actorId)
    {
        if (userId != actorId) throw new ForbiddenException(ErrorCodes.Forbidden, "Chỉ người nhận hoặc người gửi được thực hiện thao tác này.");
    }
    public static void RequireVersion(IHasConcurrencyToken entity, Guid token)
    {
        if (token == Guid.Empty || entity.ConcurrencyToken != token)
            throw new ConflictException(ErrorCodes.ConcurrencyConflict, "Dữ liệu đã thay đổi hoặc thiếu phiên bản. Vui lòng tải lại.");
    }
}

public sealed class TeamCommandServices(ITeamRepository repository, ISystemSettingsRepository settingsRepository, IUnitOfWork unitOfWork)
{
    public async Task<Domain.Entities.Team> RequireTeamAsync(Guid id, CancellationToken cancellationToken, Guid? invitationId = null, Guid? joinRequestId = null) =>
        await repository.GetForUpdateAsync(id, cancellationToken, invitationId, joinRequestId) ?? throw new NotFoundException(ErrorCodes.TeamNotFound, "Không tìm thấy đội.");
    public async Task<Domain.Entities.SystemSettings> GetSettingsAsync(CancellationToken cancellationToken) =>
        await settingsRepository.GetAsync(cancellationToken) ?? throw new NotFoundException(ErrorCodes.SystemSettingsNotFound, "Không tìm thấy cấu hình hệ thống.");
    public async Task RequireActiveUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!await repository.IsActiveUserAsync(userId, cancellationToken))
            throw new NotFoundException(ErrorCodes.UserNotFound, "Không tìm thấy người dùng đang hoạt động.");
    }
    public async Task SaveAsync(Domain.Entities.Team team, ITeamTransaction transaction, CancellationToken cancellationToken)
    {
        repository.TrackNewChildren(team);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
