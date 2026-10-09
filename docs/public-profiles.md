# Hồ sơ công khai người chơi và đội

Public nghĩa là được chia sẻ với người dùng đã đăng nhập có permission, không phải
endpoint anonymous. Không cần là thành viên đội hoặc chính chủ để đọc.

| Endpoint | Permission |
|---|---|
| GET /api/v1/users/{userId}/public-profile | profiles.public.read |
| GET /api/v1/teams/{teamId}/public-profile | teams.public_profile.read |

Hai quyền seed mặc định cho USER và ADMIN, không có ADMIN bypass; quyền bị thu hồi
không được seed phục hồi. Controller lấy actor qua ICurrentUser, query qua MediatR.
Query chỉ đọc, không commit, projection AsNoTracking ở Infrastructure.

Người chơi: userId, displayName, avatarUrl, defaultArea, sportProfiles
(môn/code/name, level tự khai, bio và các thể thức ưa thích đang hoạt động).
Chỉ tài khoản ACTIVE, chưa xoá, đã xác minh email và có profile được chia sẻ.
Môn/thể thức đã ngừng hoạt động không xuất hiện trong sportProfiles.

Đội: teamId, name, description, avatarUrl, homeArea, sport, captain
(userId/displayName/avatarUrl) và activeMemberCount. Chỉ đội ACTIVE, chưa xoá,
môn đang hoạt động và đội trưởng có hồ sơ có thể chia sẻ. Count chỉ thành viên
ACTIVE với user ACTIVE chưa xoá, không trả danh sách hay lịch sử thành viên.
Đội INACTIVE/ARCHIVED hoặc tài khoản không đủ điều kiện trả 404 như ID không tồn tại.

DTO công khai là projection riêng, không dùng UserProfileDto/TeamDto nội bộ.
Không trả email, phone, ContactNote, role/permission, session, token concurrency,
audit actor, lời mời/yêu cầu hoặc lịch sử rời/loại đội. Nội dung giới thiệu là phần
người dùng chủ động chia sẻ; UI cần render text an toàn.
Chưa có list/search công khai hoặc dữ liệu rating/thành tích. Collection môn/thể thức
thuộc chi tiết hồ sơ nên không phải endpoint list độc lập.

Kiểm thử PostgreSQL kiểm tra projection, target khác actor, thiếu/không hoạt động,
privacy và permission pipeline. Xem [testing](testing.md).
