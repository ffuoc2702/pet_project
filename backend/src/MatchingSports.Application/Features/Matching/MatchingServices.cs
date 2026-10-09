using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using MatchingSports.Domain.Entities;
using MatchingSports.Domain.Enums;
using MatchingSports.Domain.ValueObjects;
using MatchingSports.Shared;

namespace MatchingSports.Application.Features.Matching;

public sealed class MatchingServices(IMatchingRepository repository, ITeamRepository teams,
    ISystemSettingsRepository settings, IUnitOfWork unitOfWork)
{
    public async Task<Domain.Entities.SystemSettings> SettingsAsync(CancellationToken cancellationToken) =>
        await settings.GetAsync(cancellationToken) ?? throw new NotFoundException(ErrorCodes.SystemSettingsNotFound, "Không tìm thấy cấu hình hệ thống.");
    public async Task<Listing> ListingAsync(Guid id, bool tracked, CancellationToken cancellationToken) =>
        await repository.GetListingAsync(id, tracked, cancellationToken) ?? throw new NotFoundException(ErrorCodes.ListingNotFound, "Không tìm thấy kèo.");
    public async Task<Listing> MatchListingAsync(Guid id, bool tracked, CancellationToken cancellationToken) =>
        await repository.GetListingByMatchAsync(id, tracked, cancellationToken) ?? throw new NotFoundException(ErrorCodes.MatchNotFound, "Không tìm thấy trận.");
    public async Task<ListingWatch> WatchAsync(Guid id, bool tracked, CancellationToken cancellationToken) =>
        await repository.GetWatchAsync(id, tracked, cancellationToken) ?? throw new NotFoundException(ErrorCodes.ListingWatchNotFound, "Không tìm thấy theo dõi.");
    public async Task RequireActiveUserAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await teams.IsActiveUserAsync(id, cancellationToken)) throw new NotFoundException(ErrorCodes.UserNotFound, "Người dùng không hoạt động.");
    }
    public async Task RequireFormatAsync(Guid sportId, Guid? formatId, CancellationToken cancellationToken)
    {
        if (!await repository.IsActiveFormatAsync(sportId, formatId, cancellationToken))
            throw new ConflictException(ErrorCodes.InvalidMatchingOperation, "Môn hoặc thể thức không hoạt động hoặc không thuộc môn.");
    }
    public async Task<Domain.Entities.Team> RequireTeamManagerAsync(Guid teamId, Guid actorId, Guid sportId,
        CancellationToken cancellationToken, bool captainOnly = false, bool requireActive = true)
    {
        var team = await teams.GetByIdAsync(teamId, cancellationToken) ?? throw new NotFoundException(ErrorCodes.TeamNotFound, "Không tìm thấy đội.");
        if (captainOnly ? !team.IsCaptain(actorId) : !team.CanManageMembers(actorId))
            throw new ForbiddenException(ErrorCodes.Forbidden, captainOnly ? "Chỉ đội trưởng được hủy trận của đội." : "Chỉ đội trưởng hoặc đội phó được đại diện đội.");
        if (team.SportId != sportId || (requireActive && team.Status != TeamStatus.ACTIVE))
            throw new ConflictException(ErrorCodes.InvalidMatchingOperation, "Đội không hoạt động hoặc không cùng môn.");
        return team;
    }
    public async Task RequireHostAsync(Listing listing, Guid actorId, CancellationToken cancellationToken, bool captainOnly = false)
    {
        if (listing.HostTeamId is { } teamId)
            _ = await RequireTeamManagerAsync(teamId, actorId, listing.SportId, cancellationToken, captainOnly, requireActive: false);
        else RequireOwner(listing.OwnerUserId, actorId);
    }
    public async Task RequireApplicantAsync(Listing listing, ListingRequest request, Guid actorId, CancellationToken cancellationToken)
    {
        if (request.ApplicantTeamId is { } teamId)
            _ = await RequireTeamManagerAsync(teamId, actorId, listing.SportId, cancellationToken, requireActive: false);
        else RequireOwner(request.ApplicantUserId!.Value, actorId);
    }
    public async Task RequireActiveApplicantAsync(Listing listing, ListingRequest request, CancellationToken cancellationToken)
    {
        if (request.ApplicantUserId is { } userId) await RequireActiveUserAsync(userId, cancellationToken);
        else
        {
            var team = await teams.GetByIdAsync(request.ApplicantTeamId!.Value, cancellationToken);
            if (team is null || team.Status != TeamStatus.ACTIVE || team.SportId != listing.SportId)
                throw new ConflictException(ErrorCodes.InvalidMatchingOperation, "Đội đăng ký không còn hoạt động hoặc không cùng môn.");
            await RequireActiveUserAsync(team.CaptainUserId, cancellationToken);
        }
    }
    public async Task SaveAsync(Listing? listing, IMatchingTransaction transaction, CancellationToken cancellationToken)
    {
        if (listing is not null) repository.TrackChildren(listing);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
    public static void RequireOwner(Guid ownerId, Guid actorId)
    {
        if (ownerId != actorId) throw new ForbiddenException(ErrorCodes.Forbidden, "Bạn không có quyền trên tài nguyên này.");
    }
    public static void RequireVersion(IHasConcurrencyToken entity, Guid token)
    {
        if (token == Guid.Empty || token != entity.ConcurrencyToken)
            throw new ConflictException(ErrorCodes.ConcurrencyConflict, "Dữ liệu đã thay đổi hoặc thiếu phiên bản. Vui lòng tải lại.");
    }
    public static ListingRequest Request(Listing listing, Guid id) => listing.Requests.SingleOrDefault(item => item.Id == id)
        ?? throw new NotFoundException(ErrorCodes.ListingRequestNotFound, "Không tìm thấy yêu cầu của kèo.");
    public static void ValidateCriteria(ListingCriteria criteria, int maxRadiusMeters)
    {
        try { criteria.Validate(maxRadiusMeters); }
        catch (MatchingRuleException exception)
        {
            throw new RequestValidationException(ErrorCodes.InvalidMatchingOperation, "Tiêu chí tìm trận không hợp lệ.",
                new Dictionary<string, string[]> { ["criteria"] = [exception.Message] });
        }
    }
    public static void EnsureWatchCapacity(Domain.Entities.SystemSettings settings, int count)
    {
        try { settings.EnsureCanActivateListingWatch(count); }
        catch (ArgumentException exception) { throw new ConflictException(ErrorCodes.InvalidMatchingOperation, exception.GetValidationMessage()); }
    }
}
