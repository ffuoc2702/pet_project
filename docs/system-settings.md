# Cấu hình hệ thống

Backend cung cấp một bộ cấu hình toàn hệ thống trong bảng `system_settings`.
Migration seed một lần; khởi động lại hoặc chạy seed quyền không ghi đè giá trị
quản trị viên đã sửa. Không cache cấu hình trong singleton.

## API quản trị

- `GET /api/v1/system-settings`: quyền `system_settings.read`.
- `PUT /api/v1/system-settings`: quyền `system_settings.update`.

Hai quyền được cấp mặc định cho role ADMIN, không cấp cho USER. Quyền được kiểm
tra ở HTTP và MediatR pipeline. Actor lấy qua `ICurrentUser`, không có trong body.
Success trả `BaseResponse<SystemSettingsDto>`. PUT thay toàn bộ cấu hình; tất cả
15 giá trị và `concurrencyToken` đều bắt buộc. Lấy token từ GET hoặc PUT gần nhất.
Token cũ trả `CONCURRENCY_CONFLICT` (409); dữ liệu sai trả
`INVALID_SYSTEM_SETTINGS` (400), cấu hình chưa được migration tạo trả
`SYSTEM_SETTINGS_NOT_FOUND` (404).

```json
{
  "outboxMaxRetryCount": 5,
  "maxCaptainTeamsPerUser": 3,
  "maxMemberTeamsPerUser": 10,
  "maxDeputiesPerTeam": 2,
  "maxMembersPerTeam": 50,
  "teamInvitationLifetimeHours": 72,
  "maxPendingTeamRequests": 100,
  "outboxRetryInitialDelaySeconds": 5,
  "outboxRetryMaxDelaySeconds": 3600,
  "processedOutboxRetentionDays": 30,
  "notificationRetentionDays": 90,
  "maxActiveListingWatchesPerUser": 20,
  "maxListingSearchRadiusMeters": 50000,
  "maxListingWatchWindowDays": 30,
  "matchReminderLeadMinutes": 60,
  "concurrencyToken": "<token nhận từ GET>"
}
```

Mỗi PUT hợp lệ lưu một `admin_actions` loại `UPDATE_SYSTEM_SETTINGS`, gồm actor,
thời điểm và 15 giá trị trước/sau, cùng transaction với cấu hình. Token được đổi
khi lưu, kể cả gửi lại các giá trị không đổi. Hai phiên quản trị cùng đọc một
version không được ghi đè nhau; transaction thua cũng không lưu audit.

## Các thông số

| Property | Mặc định | Khoảng hợp lệ / cách hiểu |
|---|---:|---|
| `outboxMaxRetryCount` | 5 | 0–100; số lần thử lại sau lần xử lý đầu tiên. |
| `maxCaptainTeamsPerUser` | 3 | 0–10000; số team ACTIVE user làm đội trưởng. |
| `maxMemberTeamsPerUser` | 10 | 0–10000; số team ACTIVE user là thành viên/đội phó; không tính team làm đội trưởng. |
| `maxDeputiesPerTeam` | 2 | 0 đến `maxMembersPerTeam - 1`; số đội phó đồng thời. |
| `maxMembersPerTeam` | 50 | 1–10000; gồm đội trưởng, đội phó và thành viên ACTIVE. |
| `teamInvitationLifetimeHours` | 72 | 1–8760 giờ; deadline lời mời mới = thời điểm tạo + số giờ này. |
| `maxPendingTeamRequests` | 100 | 0–10000; tổng lời mời và yêu cầu tham gia PENDING chưa hết hạn của mỗi team. |
| `outboxRetryInitialDelaySeconds` | 5 | 1–604800 giây; khoảng chờ lần retry đầu. |
| `outboxRetryMaxDelaySeconds` | 3600 | Từ khoảng chờ ban đầu đến 604800 giây; giới hạn backoff. |
| `processedOutboxRetentionDays` | 30 | 1–36500 ngày; tính từ `created_at`, không tính từ `processed_at`. |
| `notificationRetentionDays` | 90 | 1–36500 ngày; tính từ `created_at`. |
| `maxActiveListingWatchesPerUser` | 20 | 0–1000; ACTIVE chưa hết hạn của mỗi user. |
| `maxListingSearchRadiusMeters` | 50000 | 1–200000 mét; giới hạn radius query/watch. |
| `maxListingWatchWindowDays` | 30 | 1–365 ngày; giới hạn cửa sổ watch từ hiện tại. |
| `matchReminderLeadMinutes` | 60 | 1–10080 phút; nhắc một lần cho trận CONFIRMED còn chưa bắt đầu. |

Giới hạn bằng 0 cho retry/team/đội phó/yêu cầu đang chờ chặn thao tác mới tương
ứng. Giảm giới hạn không tự xóa team, loại thành viên hoặc thu hồi vai trò.

## Retry outbox

Domain `SystemSettings.GetOutboxRetryDelay(retryNumber)` tính:

```text
min(initialDelaySeconds × 2^(retryNumber - 1), maxDelaySeconds)
```

Ví dụ initial = 5, max = 60: 5, 10, 20, 40, 60, 60… giây. Tính toán clamp trước
khi nhân để không overflow. `retryNumber` bắt đầu từ 1.

`OutboxMessage.RecordDeliveryFailure(error, settings, now)` dùng cấu hình hiện
tại để lên lịch lần thử lại, tăng `retry_count`, cập nhật `available_at`, lưu
`last_error` và giải phóng `locked_until`. Khi không còn lượt retry, ghi `failed_at`
và không đánh dấu thành công. Với maxRetry = 2 có tối đa ba lần gửi: lần đầu và
hai lần retry. Với maxRetry = 0, lần gửi đầu thất bại kết thúc ngay.

`CanProcess(now)` loại message đã thành công, đã hết lượt, chưa đến lịch hoặc còn
khóa. `MarkProcessed(now)` ghi thời điểm thành công, xóa lỗi và giải phóng khóa.
Worker phải lấy quyền xử lý độc quyền bằng transaction/khóa phù hợp trước khi gửi;
`CanProcess` chỉ kiểm tra trạng thái trong bộ nhớ, không tự khóa database.

## Thời điểm dọn dữ liệu

`notifications.purge_after` và `outbox_messages.purge_after` là `timestamptz`,
NOT NULL, có index phục vụ quét theo ngày. Khi thêm entity qua DbContext,
Infrastructure đọc cấu hình, hoàn tất `created_at`, rồi gọi Domain tính:

```text
notification.purge_after = notification.created_at + notificationRetentionDays
outbox.purge_after = outbox.created_at + processedOutboxRetentionDays
```

Áp dụng cho cả SaveChanges sync và async. Snapshot thời điểm dọn được lưu một lần
khi tạo; thay đổi cấu hình, đọc thông báo hoặc xử lý/retry message không tính lại
thời điểm này. Migration backfill dữ liệu cũ bằng giá trị mặc định được seed.
Các producer SQL trực tiếp phải cung cấp `purge_after` tương ứng.

Worker chạy hàng ngày có thể dùng các điều kiện:

```sql
-- Chỉ dọn outbox đã xử lý thành công, giữ message pending và failed.
SELECT id FROM outbox_messages
WHERE processed_at IS NOT NULL AND purge_after <= now();

SELECT id FROM notifications WHERE purge_after <= now();
```

Không xóa message outbox chưa xử lý chỉ vì đã qua `purge_after`. Message xử lý
thành công sau thời điểm này đủ điều kiện dọn ở lần quét kế tiếp. Nếu dùng EF
để dọn notifications, cần cân nhắc `IgnoreQueryFilters()` để không bỏ sót bản
ghi của người dùng đã soft-delete. Worker nên dọn theo batch để tránh transaction
quá lớn.

## Tích hợp các luồng team và worker

Tại thời điểm thêm feature này, repository chưa có use case/API team, model lời
mời tham gia team hoặc worker gửi outbox/dọn dữ liệu. Feature này triển khai API
cấu hình, persistence, quy tắc Domain và schema phục vụ các luồng đó.

Khi thêm use case team, Application tải cấu hình qua `ISystemSettingsRepository`,
tải số lượng hiện tại và gọi các guard Domain:

- `EnsureCanCaptainAnotherTeam`: tạo team, nhận chuyển giao hoặc kích hoạt team.
- `EnsureCanJoinAnotherTeam`: nhận lời mời/duyệt yêu cầu; tính cả chỗ đội trưởng
  trong tổng thành viên, không đếm trùng đội trưởng.
- `EnsureCanDelegateAnotherDeputy`: ủy quyền thêm đội phó.
- `EnsureCanCreateTeamRequest`: tạo lời mời/yêu cầu, dùng tổng đang chờ của team.
- `GetTeamInvitationExpiresAt`: tính deadline lời mời mới.

Không tính team không ACTIVE/đã xóa, thành viên đã rời/bị loại hoặc yêu cầu đã
hết hạn. Các guard nhận số đếm từ Application và không gọi I/O. Use case phải
bảo vệ việc đếm và ghi khỏi các thao tác đồng thời bằng transaction/khóa phù hợp.
Enum hiện tại chưa có đội phó; vai trò và quyền ủy quyền được bổ sung cùng use case
team. Lời mời hiện có trong tương lai giữ deadline đã chốt khi tạo.

## Kiểm thử

`SystemSettingsDomainTests` kiểm tra invariant, capacity, deadline, backoff và
retry hết lượt. `SystemSettingsTests` gửi request qua MediatR để kiểm tra quyền,
version, validation, audit, commit và cancellation token. `SystemSettingsPersistenceTests`
kiểm tra migration/backfill/rollback, retention sync/async, index và transaction
concurrency trên PostgreSQL/PostGIS trong database dùng riêng.

Đặt `MATCHINGSPORTS_RBAC_TEST_CONNECTION` trỏ đến database test có tên bắt đầu
`matchingsports_rbac_test_` để bật integration test; fixture cấu hình tạo database
riêng và xóa sau khi chạy. Không đặt biến này trỏ vào database phát triển.

## Tích hợp tìm kèo

Các use case watch dùng EnsureCanActivateListingWatch và transaction Serializable
để bảo vệ quota. Ba giới hạn matching được migration bổ sung với mặc định;
không thay đổi các giá trị quản trị hiện có. Xem [matching](matching.md).

Nhắc lịch đọc setting hiện tại mỗi batch; thay đổi khoảng nhắc áp dụng cho trận chưa được nhắc.
Trận đã nhắc không gửi lại khi sửa setting hoặc cleanup history. Xem [matching](matching.md).
