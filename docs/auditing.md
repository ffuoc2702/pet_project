# Entity audit và concurrency

`AuditableEntity` chứa bốn property `CreatedAt`, `UpdatedAt`, `CreatedBy`,
`UpdatedBy` với private setter. `ConcurrentAuditableEntity` kế thừa base đó và
thêm GUID `ConcurrencyToken`, triển khai `IHasConcurrencyToken`. Không có `Id`
trong base, nên khóa ghép vẫn được giữ nguyên.

EF không map hai base thành entity/bảng riêng. Mỗi entity nghiệp vụ là một root
độc lập có các cột audit; không có TPH, discriminator hoặc bảng audit base.
Infrastructure vẫn cấu hình `.IsConcurrencyToken()` và xoay GUID token khi ghi.
Domain có thể gọi `RenewConcurrencyToken()` để giữ kiểm tra version cho thao tác
không đổi giá trị. Conflict vẫn trả `CONCURRENCY_CONFLICT` qua pipeline hiện có.

## Phạm vi áp dụng

- `ConcurrentAuditableEntity`: Listing, ListingRequest, Match, AuthSession, SystemSettings, NotificationConfig.
- `AuditableEntity`: Sport, SportFormat, Permission, UserProfile, UserSportProfile,
  Team, TeamMember, MatchParticipant, Report, NotificationHistory, OtpChallenge, OutboxMessage.
- User/Role: giữ base Identity và concurrency stamp, triển khai `IAuditableEntity`.
- AdminAction: lịch sử bất biến, giữ actor/timestamp riêng hiện tại.
- RolePermission, UserSportProfileFormat và bảng liên kết Identity: giữ cấu trúc riêng.

## Nguồn actor

`IAuditActorContext` là abstraction Application, implementation dùng AsyncLocal
và đăng ký scoped. `BeginScope(Guid?)` trả IDisposable: dùng `using` để khôi phục
actor trước đó, kể cả khi exception/cancellation. Scope hỗ trợ luồng async lồng
nhau và không làm rò actor sang request hoặc task khác.

Sau khi kiểm tra quyền thành công, `AuditActorBehavior` mở scope bằng
`IPermissionAuthorizedRequest.ActorUserId`. Controller cung cấp ID qua
`ICurrentUser`; caller ngoài HTTP cung cấp actor tin cậy. UpdateUserProfile không
có ActorUserId trong command: handler lấy ID qua ICurrentUser rồi mở scope.
Actor scope không thay thế authentication, permission hoặc kiểm tra quyền dữ liệu.

IdentityService xóa actor trong scope các entrypoint anonymous. Đăng ký, yêu cầu
OTP, OTP sai và password sai có actor null. Sau khi xác minh OTP/password/refresh
token hoặc external provider, các lần ghi user/challenge/session dùng ID đã được
xác minh. Worker và seed mặc định null; không lấy user mục tiêu làm actor và không
dùng Guid.Empty. Context chỉ chứa UUID, không chứa password, OTP hoặc token.

## Persistence và response

DbContext stamp cả SaveChanges sync/async bằng TimeProvider:

| Trạng thái | CreatedAt / CreatedBy | UpdatedAt / UpdatedBy |
|---|---|---|
| Added | UTC hiện tại / actor hiện tại | UTC hiện tại / actor hiện tại |
| Modified | Giữ giá trị gốc | UTC hiện tại / actor hiện tại |

CreatedAt/CreatedBy được khôi phục từ OriginalValue và không đưa vào UPDATE,
kể cả entity bị đánh dấu Modified toàn bộ. Detached update phải tải bản ghi
hiện tại như convention repository; không attach một payload HTTP thành entity
rồi coi metadata do client gửi là giá trị gốc. Raw SQL, ExecuteUpdate và
ExecuteDelete không đi qua SaveChanges; nếu thêm các luồng này phải xử lý audit
và concurrency riêng, không giả định convention tự chạy.

Resource DTO cho sport/format, profile, sport profile, role, permission và system
settings kế thừa `BaseDto` để trả bốn trường audit. `ConcurrentBaseDto : BaseDto`
thêm `ConcurrencyToken`, dùng cho `SystemSettingsDto`. Base và DTO kế thừa đều
là record; metadata không lặp lại trong constructor DTO feature. `RoleDto` giữ
`ConcurrencyStamp` riêng của Identity. Handler map response sau commit, nên timestamp và
actor đúng với database. Request không có trường audit. Response auth/token và
DTO tổng hợp role assignment giữ contract riêng hiện tại.
Danh sách role và ma trận quyền dùng UpdatedAt để lọc FromDate/ToDate, bao gồm
hai giá trị biên và áp dụng filter trước khi count/phân trang.

AdminAction tiếp tục ghi lịch sử trong cùng transaction. Snapshot role mô tả
giá trị nghiệp vụ, bỏ metadata audit cấp trên chưa được stamp trước commit;
actor và thời điểm thao tác đã có trên bản ghi AdminAction.

NotificationHistory/outbox tính PurgeAfter sau khi CreatedAt được stamp. Khi cập nhật,
CreatedAt và deadline retention không bị tính lại.
Config/history DTO kế thừa base audit/concurrency tương ứng; luồng phát thông báo,
mobile metadata và worker được mô tả trong [notifications](notifications.md).

## Migration

Migration mới thêm actor nullable cho 19 entity có audit, thêm UpdatedAt cho
notifications/outbox_messages/otp_challenges và thêm timestamps cho roles.
Timestamp đã có được giữ nguyên; UpdatedAt mới của notification/outbox/OTP
được backfill bằng CreatedAt. Role cũ không có timestamp lịch sử: hai timestamp
mới dùng thời điểm migration, không thể suy ra thời điểm tạo thực tế.
Actor của dữ liệu cũ là null, không suy luận từ owner hoặc nhật ký gần nhất.
Không thêm FK cho actor, để metadata lịch sử tồn tại sau khi user bị xóa.

Kiểm thử gồm model không tạo bảng base, scope async/nested, creator bất biến,
API response sau commit, actor khác user mục tiêu, Identity trước/sau xác minh,
và migration/retention/concurrency trên PostgreSQL dùng database test riêng.
