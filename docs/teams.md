# Quản lý đội và thành viên

## Quyền và mô hình

Actor lấy từ ICurrentUser ở controller, permission được kiểm tra qua MediatR; không nhận actor từ body.
Quyền hệ thống không thay thế quyền trên đội. Đội trưởng quản lý thông tin, đội phó và chuyển quyền.
Đội phó chỉ mời người, xử lý yêu cầu và loại thành viên thường. Chỉ người nhận được phản hồi lời mời;
chỉ người gửi được rút yêu cầu. Thành viên được rời đội; đội trưởng phải chuyển quyền trước.

Team kế thừa ConcurrentAuditableEntity; TeamInvitation và TeamJoinRequest có audit và concurrency riêng.
TeamMember giữ khóa ghép (TeamId, UserId). Tạo đội tạo membership CAPTAIN. Khi quay lại đội, tái sử dụng
membership lịch sử, đặt lại role MEMBER, JoinedAt và LeftAt. Đội trưởng hiện tại luôn khớp CaptainUserId.

Chuyển đội trưởng chỉ nhận thành viên ACTIVE (kể cả DEPUTY). Người cũ trở thành MEMBER; người nhận
trở thành CAPTAIN. Kiểm tra quota của cả hai người trước khi thay đổi. Số đội làm đội trưởng và số đội
làm thành viên không tính trùng: membership trong đội do mình làm đội trưởng không tiêu thụ quota MEMBER.
Đội chưa xóa và chưa ARCHIVED vẫn tiêu thụ quota, kể cả INACTIVE.

SystemSettings là nguồn giới hạn. MaxMembersPerTeam tính cả đội trưởng. MaxPendingTeamRequests
là tổng lời mời còn hiệu lực và yêu cầu PENDING của đội, theo invariant SystemSettings hiện có.
Lời mời dùng TeamInvitationLifetimeHours; tại ExpiresAt đã hết hiệu lực. Query phản ánh EXPIRED mà
không commit; lần ghi tiếp theo chuẩn hóa trạng thái hết hạn. Không tự phát thông báo khi thời gian trôi qua.

## Transaction và xóa đội

Tất cả lệnh ghi mở transaction Serializable; repository không tự SaveChanges.
Việc kiểm tra quota, thành viên và pending, ghi nghiệp vụ, NotificationHistory, outbox và audit diễn ra
trong cùng transaction. Serialization failure, deadlock và optimistic concurrency conflict trả
CONCURRENCY_CONFLICT; không tự retry lệnh ghi hoặc ghi đè bằng phiên bản cũ.

Lệnh cập nhật thông tin, đội phó, loại thành viên, chuyển đội trưởng và xóa yêu cầu ConcurrencyToken
của Team. Phản hồi lời mời/yêu cầu dùng token của lời mời/yêu cầu. Tạo lời mời, tạo yêu cầu và rời đội
không yêu cầu token client nhưng vẫn được bảo vệ bằng transaction và concurrency persistence.

Chỉ đội trưởng được xóa khi thành viên ACTIVE duy nhất là đội trưởng. LEFT/REMOVED giữ lịch sử và
không cản xóa. Xóa là soft delete (DeletedAt, ARCHIVED); đội còn kèo hoặc trận chưa kết thúc không được xóa.
Lời mời/yêu cầu PENDING bị kết thúc cùng transaction. Mỗi người bị ảnh hưởng nhận tối đa một thông báo
xóa đội, kể cả có cả lời mời và yêu cầu. Các query team ẩn đội đã xóa; hộp notification vẫn giữ snapshot.

## API

Các endpoint yêu cầu authentication và policy khớp command/query. Mọi danh sách dùng BaseRequest,
BaseQuery, phân trang tại database, tìm kiếm và filter UpdatedAt hai biên bao gồm.

| Endpoint | Hành vi |
|---|---|
| GET /api/v1/teams | Đội ACTIVE; MineOnly=true lấy đội của mình; filter SportId |
| POST /api/v1/teams | Tạo đội |
| GET /api/v1/teams/{teamId} | Xem đội ACTIVE; đội INACTIVE chỉ thành viên được xem |
| PUT /api/v1/teams/{teamId} | Sửa thông tin |
| DELETE /api/v1/teams/{teamId} | Xóa đội chỉ còn đội trưởng |
| GET /api/v1/teams/{teamId}/members | Thành viên ACTIVE; chỉ thành viên đội được xem |
| POST /api/v1/teams/{teamId}/leave | Rời đội |
| DELETE /api/v1/teams/{teamId}/members/{userId} | Loại thành viên |
| POST /api/v1/teams/{teamId}/members/{userId}/deputy | Bổ nhiệm đội phó |
| DELETE /api/v1/teams/{teamId}/members/{userId}/deputy | Thu hồi đội phó |
| POST /api/v1/teams/{teamId}/captain/transfer | Chuyển đội trưởng |
| GET /api/v1/team-invitations | Lời mời của chính mình |
| GET /api/v1/teams/{teamId}/invitations | Lời mời đội; chỉ ban quản lý |
| POST /api/v1/teams/{teamId}/invitations | Mời UserId |
| POST /api/v1/teams/{teamId}/invitations/{invitationId}/accept | Chấp nhận |
| POST /api/v1/teams/{teamId}/invitations/{invitationId}/reject | Từ chối |
| POST /api/v1/teams/{teamId}/invitations/{invitationId}/cancel | Thu hồi |
| GET /api/v1/team-join-requests | Yêu cầu của chính mình |
| GET /api/v1/teams/{teamId}/join-requests | Yêu cầu đội; chỉ ban quản lý |
| POST /api/v1/teams/{teamId}/join-requests | Gửi yêu cầu với Message tùy chọn |
| POST /api/v1/teams/{teamId}/join-requests/{joinRequestId}/accept | Duyệt |
| POST /api/v1/teams/{teamId}/join-requests/{joinRequestId}/reject | Từ chối |
| POST /api/v1/teams/{teamId}/join-requests/{joinRequestId}/withdraw | Rút |

Lệnh có version nhận body { "concurrencyToken": "UUID" }. Chuyển đội trưởng nhận thêm UserId.
Danh sách lời mời/yêu cầu nhận Status tùy chọn. Token và audit trả trong DTO sau commit.

## Thông báo

In-app mặc định; email bật bằng Teams:Notifications:EmailEnabled=true và cần SMTP đã cấu hình.
Push/SMS không được bật. TeamNotifications tạo history độc lập (NotificationConfigId=null), snapshot
text được HTML-encode, BodyText dành cho mobile, metadata SchemaVersion=1 và điều hướng tới TEAM.
Xóa đội dùng OPEN_NOTIFICATIONS để không điều hướng tới tài nguyên đã xóa.

Tạo/xóa đội gửi xác nhận cho actor. Các action khác bỏ actor khỏi tập người nhận, deduplicate theo user
và chỉ nhận user ACTIVE chưa xóa. Mời/thu hồi gửi người nhận; phản hồi lời mời gửi người mời và quản lý;
yêu cầu/rút gửi quản lý, duyệt/từ chối gửi người yêu cầu; thay đổi vai trò/loại gửi người bị ảnh hưởng và
quản lý; chuyển đội trưởng/sửa thông tin gửi các thành viên còn lại. Gia nhập qua một luồng kết thúc
các pending tương ứng ở luồng khác và phát thông báo kết thúc.

Email dùng NotificationEmailRequested với AggregateType=TEAM và AggregateId=Team.Id. Dispatcher kiểm
tra liên kết history/outbox riêng cho team và notification config, tái sử dụng retry, retention và cleanup.
Email thông báo nghiệp vụ đã commit vẫn được gửi sau khi đội bị xóa; đây là snapshot, không phải lịch gửi
cần hủy theo trạng thái đội. SMTP không đảm bảo exactly-once.

## Migration và kiểm thử

AddTeamManagement thêm token cho teams, hai bảng lời mời/yêu cầu và unique index PENDING.
Migration bổ sung/sửa membership CAPTAIN cho đội chưa xóa, chuyển CAPTAIN khác thành MEMBER.
Membership mới dùng timestamp migration và actor null; không suy luận actor từ CaptainUserId.
Down bỏ hai bảng và token, không đảo ngược sửa dữ liệu membership.

TeamDomainTests kiểm tra invariant và normalization; TeamAuthorizationTests đi qua MediatR.
TeamPersistenceTests dùng PostgreSQL riêng với MATCHINGSPORTS_RBAC_TEST_CONNECTION, tạo database tên
matchingsports_rbac_test_team_* và dọn sau test. Bao gồm ownership, deputy, transfer, delete, paging,
rollback, recipient/audit, standalone email và cạnh tranh sức chứa/quota giữa nhiều đội.
