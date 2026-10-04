using MatchingSports.Shared;

namespace MatchingSports.Infrastructure.Persistence.Seed;

public sealed record InitialPermissionDefinition(string Code, string Name, bool GrantedToUser, string? LegacyReadCode = null);

public static class InitialPermissionCatalog
{
    public static IReadOnlyList<InitialPermissionDefinition> All { get; } = Array.AsReadOnly<InitialPermissionDefinition>(
    [
        new(PermissionCodes.ReadOwnProfile, "Xem hồ sơ của mình", true),
        new(PermissionCodes.UpdateOwnProfile, "Cập nhật hồ sơ của mình", true),
        new(PermissionCodes.ListSports, "Xem danh sách môn thể thao", true, PermissionCodes.ReadSports),
        new(PermissionCodes.ReadSports, "Xem chi tiết môn thể thao", true),
        new(PermissionCodes.CreateSports, "Tạo môn thể thao", false),
        new(PermissionCodes.UpdateSports, "Cập nhật môn thể thao", false),
        new(PermissionCodes.DeactivateSports, "Vô hiệu hóa môn thể thao", false),
        new(PermissionCodes.ListRoles, "Xem danh sách role", false, PermissionCodes.ReadRoles),
        new(PermissionCodes.ReadRoles, "Xem chi tiết role và quyền được gán", false),
        new(PermissionCodes.CreateRoles, "Tạo role", false),
        new(PermissionCodes.UpdateRoles, "Cập nhật và vô hiệu hóa role", false),
        new(PermissionCodes.AssignUserRole, "Gán role cho người dùng", false),
        new(PermissionCodes.ListPermissions, "Xem danh sách permission", false, PermissionCodes.ReadPermissions),
        new(PermissionCodes.ReadPermissions, "Xem chi tiết permission", false),
        new(PermissionCodes.UpdateRolePermissions, "Gán/gỡ permission của role", false),
        new(PermissionCodes.ReadSportProfiles, "Xem chi tiết hồ sơ môn của mình", true),
        new(PermissionCodes.ListSportProfiles, "Xem danh sách hồ sơ môn của mình", true, PermissionCodes.ReadSportProfiles),
        new(PermissionCodes.UpdateSportProfiles, "Cập nhật hồ sơ môn của mình", true),
        new(PermissionCodes.ReadSportFormats, "Xem chi tiết thể thức thi đấu", true),
        new(PermissionCodes.ListSportFormats, "Xem danh sách thể thức thi đấu", true, PermissionCodes.ReadSportFormats),
        new(PermissionCodes.CreateSportFormats, "Tạo thể thức thi đấu", false),
        new(PermissionCodes.UpdateSportFormats, "Cập nhật thể thức thi đấu", false),
        new(PermissionCodes.DeactivateSportFormats, "Vô hiệu hóa thể thức thi đấu", false),
        new(PermissionCodes.ReadTeams, "Xem chi tiết đội được phép truy cập", true),
        new(PermissionCodes.ListTeams, "Xem danh sách đội được phép truy cập", true, PermissionCodes.ReadTeams),
        new(PermissionCodes.CreateTeams, "Tạo đội", true),
        new(PermissionCodes.UpdateTeams, "Cập nhật đội mình quản lý", true),
        new(PermissionCodes.ArchiveTeams, "Lưu trữ đội mình quản lý", true),
        new(PermissionCodes.AddTeamMembers, "Thêm thành viên vào đội mình quản lý", true),
        new(PermissionCodes.RemoveTeamMembers, "Xóa thành viên khỏi đội mình quản lý", true),
        new(PermissionCodes.ListListings, "Tìm kiếm và xem danh sách kèo", true, PermissionCodes.ReadListings),
        new(PermissionCodes.ReadListings, "Xem chi tiết kèo", true),
        new(PermissionCodes.CreateListings, "Tạo kèo", true),
        new(PermissionCodes.UpdateListings, "Cập nhật kèo của mình", true),
        new(PermissionCodes.CloseListings, "Đóng nhận yêu cầu cho kèo của mình", true),
        new(PermissionCodes.CancelListings, "Hủy kèo của mình", true),
        new(PermissionCodes.ReadRequests, "Xem chi tiết yêu cầu mình gửi hoặc nhận", true),
        new(PermissionCodes.ListRequests, "Xem danh sách yêu cầu mình gửi hoặc nhận", true, PermissionCodes.ReadRequests),
        new(PermissionCodes.CreateRequests, "Gửi yêu cầu tham gia/thách đấu", true),
        new(PermissionCodes.WithdrawRequests, "Rút yêu cầu mình đã gửi", true),
        new(PermissionCodes.AcceptRequests, "Chấp nhận yêu cầu đến kèo mình quản lý", true),
        new(PermissionCodes.RejectRequests, "Từ chối yêu cầu đến kèo mình quản lý", true),
        new(PermissionCodes.ReadMatches, "Xem chi tiết trận được phép truy cập", true),
        new(PermissionCodes.ListMatches, "Xem danh sách trận được phép truy cập", true, PermissionCodes.ReadMatches),
        new(PermissionCodes.ConfirmMatches, "Xác nhận tham gia trận", true),
        new(PermissionCodes.CompleteMatches, "Xác nhận hoàn thành trận", true),
        new(PermissionCodes.CancelMatches, "Hủy trận mình tổ chức", true),
        new(PermissionCodes.ReportMatchNoShow, "Báo người tham gia vắng mặt", true),
        new(PermissionCodes.ReadMatchContact, "Xem liên hệ sau khi ghép thành công", true),
        new(PermissionCodes.ReadNotifications, "Xem chi tiết thông báo của mình", true),
        new(PermissionCodes.ListNotifications, "Xem danh sách thông báo của mình", true, PermissionCodes.ReadNotifications),
        new(PermissionCodes.MarkNotificationsRead, "Đánh dấu thông báo của mình đã đọc", true),
        new(PermissionCodes.CreateReports, "Gửi báo cáo vi phạm", true),
        new(PermissionCodes.ReadOwnReports, "Xem chi tiết báo cáo mình đã gửi", true),
        new(PermissionCodes.ListOwnReports, "Xem danh sách báo cáo mình đã gửi", true, PermissionCodes.ReadOwnReports),
        new(PermissionCodes.ReadUsers, "Xem chi tiết người dùng phục vụ quản trị", false),
        new(PermissionCodes.ListUsers, "Xem danh sách người dùng phục vụ quản trị", false, PermissionCodes.ReadUsers),
        new(PermissionCodes.WarnUsers, "Cảnh báo người dùng", false),
        new(PermissionCodes.SuspendUsers, "Khóa tài khoản", false),
        new(PermissionCodes.ReactivateUsers, "Mở lại tài khoản", false),
        new(PermissionCodes.ModerateListings, "Xem và ẩn kèo vi phạm", false),
        new(PermissionCodes.CancelAnyMatch, "Hủy trận phục vụ xử lý vi phạm", false),
        new(PermissionCodes.ReadAllReports, "Xem chi tiết báo cáo trong phạm vi quản trị", false),
        new(PermissionCodes.ListAllReports, "Xem danh sách toàn bộ báo cáo", false, PermissionCodes.ReadAllReports),
        new(PermissionCodes.ResolveReports, "Xử lý hoặc bác bỏ báo cáo", false),
        new(PermissionCodes.ReadAuditLogs, "Xem chi tiết thao tác quản trị", false),
        new(PermissionCodes.ListAuditLogs, "Xem danh sách lịch sử thao tác quản trị", false, PermissionCodes.ReadAuditLogs)
    ]);

    public static IEnumerable<string> GetDefaultPermissionCodes(string roleName) =>
        All.Where(definition => roleName == RoleNames.Admin ||
            (roleName == RoleNames.User && definition.GrantedToUser)).Select(definition => definition.Code);
}
