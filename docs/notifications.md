# Thông báo quản trị và hộp thông báo web/mobile

## Model và invariant

- NotificationConfig kế thừa ConcurrentAuditableEntity; DTO kế thừa ConcurrentBaseDto.
  Hai junction NotificationConfigUser/NotificationConfigTeam dùng khóa ghép, không có base audit/token.
- NotificationHistory kế thừa AuditableEntity và thay thế Notification cũ. Một history là
  một user × một lượt; unique (NotificationConfigId, SendNumber, UserId).
- UserId có giá trị khi phát thông báo; FK SetNull giữ lịch sử khi hard-delete user.
  TeamId chỉ có giá trị khi có đúng một team nguồn; Metadata.SourceTeamIds giữ toàn bộ nguồn.
- AudienceType: SELECTED_USERS / SELECTED_TEAMS / ALL_USERS. Chỉ chọn một loại đối tượng.
  Người nhận là user ACTIVE chưa bị soft-delete. Team ACTIVE, chưa bị xóa; bao gồm đội trưởng
  và thành viên ACTIVE. User nằm trong nhiều team vẫn nhận một history.
- Danh sách người nhận chốt theo từng lượt trong transaction RepeatableRead. Lượt sau
  dùng thành viên hiện tại. Lượt không còn người nhận vẫn tiêu thụ một SendNumber.
- Snapshot Title/Body/BodyText/Critical/Metadata giữ nguyên khi sửa config.
  Type/EntityType/EntityId của thông báo nghiệp vụ cũ tiếp tục được hỗ trợ.
  NotificationConfigId null dành cho thông báo nghiệp vụ độc lập.
- Critical là INFO / WARN / CRIT, độc lập kênh gửi và NotificationType.

## Nội dung và metadata

Body là HTML được làm sạch bằng HtmlSanitizer với danh sách thẻ/thuộc tính cho phép
(hyperlink HTTP/HTTPS; không script, style, event handler hoặc ảnh từ xa).
BodyText lấy từ DOM sau khi làm sạch, dành cho mobile/push/SMS.

HTTP Metadata là JSON object, không phải chuỗi JSON được encode lần nữa.
Các khóa chuẩn chấp nhận camelCase hoặc PascalCase từ client và được lưu/trả về
PascalCase; không chấp nhận hai khóa cùng nghĩa trong một object.
Domain lưu JSON và kiểm tra kích thước tối đa 16000 ký tự, không khóa trùng,
SchemaVersion = 1. ActionType hỗ trợ NONE, VIEW_ENTITY, OPEN_NOTIFICATIONS.
VIEW_ENTITY yêu cầu EntityType và EntityId UUID khác Guid.Empty.
Web/mobile ánh xạ ActionType + EntityType + EntityId sang màn hình; không chạy code từ metadata.
SourceTeamIds do server ghi và không tin giá trị client cung cấp.

Thông báo nghiệp vụ cũ dùng text: khi tạo history độc lập, giữ text trong BodyText,
escape HTML và bổ sung SchemaVersion trong bước khởi tạo retention.
Không ghi HTML chưa làm sạch vào config hoặc sửa snapshot đã phát.

## Lịch và trạng thái

Cron có **5 trường** (phút, giờ, ngày, tháng, thứ), được tính bằng Cronos với TimeZoneId
IANA, mặc định API Asia/Ho_Chi_Minh. Timestamp lưu UTC; không phụ thuộc múi giờ máy worker.

- DRAFT không phát thông báo; kích hoạt mới bắt đầu lập lịch.
- ONCE yêu cầu MaxSendTimes = 1 và CronString null.
- SCHEDULED yêu cầu CronString hợp lệ; MaxSendTimes null nghĩa là không giới hạn lượt.
- IsSendNow: lượt đầu tại thời điểm kích hoạt do server xác định. Với cron, lượt ngay
  được tính vào MaxSendTimes; các lượt sau là thời điểm cron tiếp theo.
- ONCE không gửi ngay yêu cầu AvailableAt trong tương lai khi kích hoạt.
- ExpiredAfter là thời điểm tuyệt đối dừng tạo/gửi, không phải thời gian lưu dữ liệu.
  History.ExpiresAt chốt hạn gửi của lượt đã tạo. Thông báo đã xuất bản trong app vẫn
  hiển thị đến PurgeAfter, kể cả cấu hình kết thúc hoặc bị hủy.
- Khi trễ lịch, worker tạo tối đa một lượt còn hiệu lực và tính lượt tiếp theo từ thời
  điểm hiện tại; không phát cả backlog.
- PENDING → SCHEDULING → PENDING khi cron còn lượt, hoặc COMPLETED khi hết lịch/giới hạn.
  COMPLETED kết thúc tạo lượt, không chứng minh mọi email đã được gửi.
- CANCEL dừng lượt tương lai; SchedulingStatus = ABORTED, AbortReason = ADMIN_CANCELLED.
  Cron/múi giờ không còn hợp lệ khi worker chạy: ABORTED, INVALID_SCHEDULE.
- ACTIVE đang SCHEDULING, đã COMPLETED hoặc ABORTED không được sửa nội dung/lịch.
  Sửa ACTIVE còn PENDING áp dụng cho lượt tương lai, có kiểm tra ConcurrencyToken.
  Không cung cấp endpoint xóa cứng cấu hình đã có lịch sử.

## Worker, transaction và kênh

Application NotificationJobs điều phối; Infrastructure giữ SQL lock, transaction,
cron/HTML adapter và SMTP. Domain không gọi database/provider.

Scheduler claim một config bằng FOR UPDATE SKIP LOCKED, ghi lease 5 phút và token.
Transaction tiếp theo khóa config, kiểm tra token/trạng thái/hạn, chốt người nhận,
ghi history + email outbox + SendCount + lịch trong cùng transaction. Claim hết hạn
được phục hồi; worker cũ không được thực hiện bằng token đã mất quyền sở hữu.

Worker email chỉ xử lý event NotificationEmailRequested, không tiêu thụ event outbox khác.
Mỗi user/lượt có một email outbox; retry giữ nguyên HistoryId/SendNumber và khóa tương quan.
Lỗi dùng SystemSettings.OutboxMaxRetryCount và backoff hiện có; retry không tăng SendCount.
Khi hết retry: FailedAt trên outbox và EmailStatus = FAILED trên history.
Không đủ điều kiện nhận, hủy hoặc hết hạn: SKIPPED. ReadAt chỉ biểu diễn đọc trong app.

Dispatcher giữ khóa hàng outbox/config/history qua ranh giới gửi SMTP để tuần tự hóa
với hủy. Tác vụ đã vào provider có thể hoàn thành trước khi hủy được commit.
SMTP timeout 60 giây; cancellation khi shutdown rollback để worker sau phục hồi.
SMTP không có đảm bảo exactly-once: header X-Notification-Id chỉ là ID tương quan.
Provider nhận email nhưng commit DB thất bại vẫn có thể dẫn tới email lặp khi retry.

Phase hiện tại: in-app + email qua Auth:Gmail (SMTP hiện có). DRAFT được lưu PushEnabled/
SmsEnabled để chuẩn bị phase sau; không kích hoạt hoặc sửa ACTIVE sang kênh chưa hỗ trợ.
UserDevice, đăng ký token và delivery theo từng thiết bị thuộc phase mobile sau.
ReadAt dùng chung user trên web/mobile; mark-read khóa hàng để giữ timestamp đầu tiên.

Worker đăng ký ở API, sau database initializer; không tự chạy trong migrator.
Notifications:WorkersEnabled mặc định true, có thể false trong test/host không chạy worker.
Poll mỗi 5 giây; gửi ngay là enqueue đến hạn, không gọi provider trong HTTP request.
Cleanup chạy theo ngày UTC, theo batch và bỏ qua hàng đang khóa.
PurgeAfter = CreatedAt + SystemSettings retention đã chốt; không tính lại khi sửa.
Không xóa history đang có email outbox chưa kết thúc. Outbox đã processed được dọn
theo retention; outbox FAILED giữ lại để điều tra, không tự chuyển thành processed.

Broadcast hiện materialize người nhận và commit một lượt trong một transaction.
Khi cần chia nhiều batch với checkpoint ở quy mô lớn, thêm NotificationRun trong phase mở rộng.

## HTTP API và permission

Tất cả endpoint yêu cầu authentication. Actor lấy từ ICurrentUser, không lấy từ body/query.
Permission pipeline bảo vệ cả caller ngoài HTTP; controller policy khớp use case.

| Endpoint | Permission |
|---|---|
| POST /api/v1/notification-configs | notification_configs.create |
| PUT /api/v1/notification-configs/{id} | notification_configs.update |
| POST /api/v1/notification-configs/{id}/activate | notification_configs.activate |
| POST /api/v1/notification-configs/{id}/cancel | notification_configs.cancel |
| GET /api/v1/notification-configs | notification_configs.list |
| GET /api/v1/notification-configs/{id} | notification_configs.read |
| POST /api/v1/notification-configs/preview | notification_configs.read |
| GET /api/v1/notification-configs/{id}/history | notification_history.list |
| GET /api/v1/notifications | notifications.list |
| GET /api/v1/notifications/unread-count | notifications.list |
| GET /api/v1/notifications/{id} | notifications.read |
| POST /api/v1/notifications/{id}/read | notifications.mark_read |

Config/history list dùng BaseRequest/BaseQuery, search Title và filter UpdatedAt
hai biên bao gồm, filter trước count/paging. Preview phân trang người nhận và trả
tối đa 5 mốc lịch. Không trả email của người nhận trong preview.

API người dùng chỉ truy cập history của chính mình có InAppEnabled và chưa đến
PurgeAfter; tham số giả actor/adminView không thay đổi phạm vi. Mark-read không
đánh dấu email đã gửi. API quản trị đọc history kể cả email-only.

Create/update/activate/cancel ghi AdminAction cùng transaction với thay đổi.
DTO map sau commit, metadata audit kế thừa base. Worker/cleanup dùng actor null.
Seed thêm quyền quản trị mới cho role ADMIN quản lý sẵn có một lần, không khôi
phục quyền đã bị thu hồi khi seed lại.

Ví dụ tạo bản nháp gửi ngay trong app:

```json
{
  "isSendNow": true,
  "scheduleType": "ONCE",
  "critical": "INFO",
  "title": "Thông báo hệ thống",
  "body": "<p>Nội dung cho thành viên</p>",
  "maxSendTimes": 1,
  "audienceType": "ALL_USERS",
  "userIds": [],
  "teamIds": [],
  "inAppEnabled": true,
  "emailEnabled": false,
  "pushEnabled": false,
  "smsEnabled": false,
  "timeZoneId": "Asia/Ho_Chi_Minh",
  "metadata": { "SchemaVersion": 1, "ActionType": "OPEN_NOTIFICATIONS" }
}
```

Lấy ConcurrencyToken từ response rồi POST /{id}/activate với
`{ "concurrencyToken": "<token>" }`. Update/cancel cũng yêu cầu token hiện tại.
Không tự retry lệnh ghi bằng token cũ khi gặp CONCURRENCY_CONFLICT.

## Migration và kiểm thử

Migration mới chuyển bảng notifications thành notification_history qua bảng tạm trong
transaction. Giữ ID, user, Type/entity links, audit, ReadAt và PurgeAfter. Body cũ được
coi là text, giữ nguyên trong BodyText và encode vào HTML; ScheduledAt = CreatedAt,
SendNumber = 0, NotificationConfigId = null. Metadata được bổ sung SchemaVersion.

Down chỉ phục hồi history độc lập còn UserId vào schema cũ. Config và history có
cấu hình không thể biểu diễn trong schema cũ và bị loại khi rollback migration;
không chạy Down trên production như cách hủy thông báo.

Tests: invariant/cron/timezone/misfire/lease, HTML/metadata, actor, snapshot, unique
recipient giữa team, ALL_USERS, transaction, retry/failure/cancel/expiry, cleanup
bảo vệ pending, đọc đồng thời, migration up/down, HTTP auth/permission/model-binding,
pagination, metadata JSON object và spoof actor.
