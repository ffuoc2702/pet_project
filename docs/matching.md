# Tìm kèo, tham gia và quản lý trận

## Phạm vi và mô hình

Listing là kèo đang tuyển; Match là trận được tạo khi yêu cầu đầu tiên được duyệt.
FIND_PLAYERS cho người chơi cá nhân đăng ký vãng lai, mỗi yêu cầu chiếm một chỗ.
FIND_OPPONENT chỉ cho team đấu team: bắt buộc có team chủ nhà, team đăng ký khác
team chủ nhà và OpenSlots = 1. Sport/Format phải đang hoạt động và cùng môn.

Domain giữ invariant/chuyển trạng thái. Application tải aggregate, kiểm tra
permission và vai trò hiện tại, gọi Domain, ghi thông báo/outbox và commit qua
IUnitOfWork. Infrastructure giữ EF, PostGIS, khóa và transaction Serializable.
Controller lấy actor từ ICurrentUser; body không có actor, owner hay audit.

## Trạng thái và quyền

- Listing: OPEN → MATCHED khi đủ chỗ → CONFIRMED → COMPLETED. CANCELLED/EXPIRED
  là trạng thái kết thúc. Yêu cầu PENDING chưa giữ chỗ; chỉ ACCEPTED tăng AcceptedSlots.
- Request: PENDING → ACCEPTED/REJECTED/WITHDRAWN; ACCEPTED có thể WITHDRAWN
  trước cutoff/start nếu trận còn PENDING. Xác nhận trận đóng các yêu cầu PENDING.
  Hủy kèo/trận chuyển yêu cầu còn hiệu lực sang CANCELLED; hết hạn sang EXPIRED.
- Match: PENDING → CONFIRMED → COMPLETED, hoặc CANCELLED. Người chơi đã rút
  có thể đăng ký lại; participant được dùng lại với SourceRequestId mới,
  còn request cũ được giữ làm lịch sử.
- Chủ kèo cá nhân quản lý kèo của mình. Với team, đội trưởng/đội phó hiện tại
  tạo/sửa kèo, duyệt/từ chối, xác nhận/hoàn tất trận. Hủy chỉ cho đội trưởng
  hiện tại; người tạo kèo trước khi chuyển đội trưởng không giữ quyền riêng.
- Người đăng ký cá nhân hoặc người quản lý team đăng ký được rút yêu cầu của mình.
  Không tự đăng ký kèo cá nhân của mình hoặc đăng ký vãng lai vào kèo của team mình.
- Chỉ xác nhận khi đủ chỗ và trước giờ bắt đầu. Sau khi đã có người được duyệt,
  không sửa chi tiết kèo. Hoàn tất chỉ sau StartAt + DurationMinutes.
- Hủy thông thường trước StartAt, bắt buộc lý do. Hủy lặp lại trả trạng thái
  hiện tại và không phát thêm thông báo. API admin có permission riêng, có thể
  hủy sau StartAt và ghi AdminAction cùng transaction.
- OPEN hết hạn tại CutoffAt; MATCHED chưa xác nhận hết hạn tại StartAt.
  Match PENDING tương ứng bị hủy, giữ lịch sử và giải phóng cam kết của team.
  Worker không suy luận actor từ chủ kèo/người nhận: audit actor là null.

Các thao tác cập nhật/rút/duyệt/xác nhận/hủy dùng ConcurrencyToken từ DTO hiện tại.
Conflict trả CONCURRENCY_CONFLICT (409); client tải lại dữ liệu trước khi quyết
định gửi lệnh mới. Không tự retry bằng token cũ. Transaction và token aggregate
bảo vệ chỗ cuối khi hai người được duyệt đồng thời, cũng như cuộc đua hủy/duyệt.

## API

Tất cả API yêu cầu authentication và permission tương ứng. List dùng phân trang
PageNumber/PageSize cùng Search, FromDate/ToDate theo UpdatedAt; filter trước count.
Chi tiết trả BaseResponse<T>, danh sách trả PaginatedResponse<T>.

| Endpoint | Hành vi |
|---|---|
| POST /api/v1/listings | Tạo kèo cá nhân hoặc team. |
| GET /api/v1/listings | Tìm kèo; MineOnly để xem kèo cá nhân/team của mình. |
| GET /api/v1/listings/{id} | Chi tiết kèo; kèo đã đóng chỉ cho bên liên quan. |
| PUT /api/v1/listings/{id} | Sửa kèo chưa có người được duyệt. |
| POST /api/v1/listings/{id}/cancel | Hủy kèo chưa có Match. |
| POST /api/v1/listings/{id}/requests | Gửi yêu cầu tham gia, ApplicantTeamId chỉ dùng FIND_OPPONENT. |
| GET /api/v1/listings/{id}/requests | Chủ kèo xem yêu cầu, có phân trang. |
| GET /api/v1/listing-requests | Yêu cầu cá nhân/team do mình quản lý, có phân trang. |
| GET /api/v1/listings/{id}/requests/{requestId} | Chi tiết cho chủ kèo hoặc bên đăng ký. |
| POST /api/v1/listings/{id}/requests/{requestId}/accept | Duyệt yêu cầu. |
| POST /api/v1/listings/{id}/requests/{requestId}/reject | Từ chối, có lý do. |
| POST /api/v1/listings/{id}/requests/{requestId}/withdraw | Rút yêu cầu. |
| GET /api/v1/matches | Trận cá nhân/team của mình, có phân trang. |
| GET /api/v1/matches/{id} | Chi tiết cho bên liên quan. |
| GET /api/v1/matches/{id}/contact | ContactNote của người chơi/đội trưởng hiện tại. |
| POST /api/v1/matches/{id}/confirm | Xác nhận trận đủ chỗ. |
| POST /api/v1/matches/{id}/cancel | Hủy trận, có lý do. |
| POST /api/v1/matches/{id}/complete | Hoàn tất sau giờ kết thúc. |
| POST /api/v1/admin/matches/{id}/cancel | Admin hủy, có audit. |
| GET, POST /api/v1/listing-watches | Liệt kê/tạo theo dõi của chính mình. |
| GET, PUT /api/v1/listing-watches/{id} | Đọc/sửa theo dõi của chính mình. |
| POST /api/v1/listing-watches/{id}/pause | Tạm dừng. |
| POST /api/v1/listing-watches/{id}/activate | Kích hoạt lại và kiểm tra quota. |
| POST /api/v1/listing-watches/{id}/cancel | Hủy theo dõi vĩnh viễn. |

Contact chỉ mở khi Match CONFIRMED và actor đang tham gia; không trả địa chỉ email.
Theo dõi không đọc/sửa hộ user khác, kể cả có permission quản lý khác.
Permission listing/request/match hiện có được dùng lại; thêm bảy permission
listing_watches.* cho USER/ADMIN qua seed một lần, không khôi phục quyền đã thu hồi.

## Tìm vãng lai theo vị trí và thời gian

GET listings hỗ trợ SportId, FormatId, Mode, Level, StartFrom/StartTo và bộ
Latitude/Longitude/RadiusMeters. Ba trường vị trí phải đi cùng nhau. Khoảng thời
gian bao gồm hai biên; ISO-8601 có offset được chuyển UTC. Tọa độ chuẩn hóa sáu
chữ số thập phân; PostGIS geography tính khoảng cách theo mét, trả DistanceMeters.
Khi có vị trí, thứ tự là khoảng cách, giờ bắt đầu, ID; nếu không có vị trí,
thứ tự là giờ bắt đầu, ID. Default chỉ trả OPEN còn chỗ, chưa cutoff/start,
với user/team/sport/format đang hoạt động. Level là cấp độ người tìm và phải
nằm trong khoảng MinLevel..MaxLevel của kèo, không so sánh thứ tự chuỗi enum.

Ví dụ: tìm bóng đá trong 2 km, bắt đầu trong khoảng tối đã chọn:

~~~http
GET /api/v1/listings?SportId=<football-id>&Mode=FIND_PLAYERS&Latitude=10.8&Longitude=106.7&RadiusMeters=2000&StartFrom=2026-10-09T18:00:00%2B07:00&StartTo=2026-10-09T22:00:00%2B07:00&PageNumber=1&PageSize=20
~~~

Nếu chưa có kèo, POST listing-watches lưu cùng tiêu chí:

~~~json
{
  "sportId": "<football-id>",
  "formatId": null,
  "latitude": 10.8,
  "longitude": 106.7,
  "radiusMeters": 2000,
  "startFrom": "2026-10-09T18:00:00+07:00",
  "startTo": "2026-10-09T22:00:00+07:00",
  "emailEnabled": false
}
~~~

Watch hiện dành cho cá nhân tìm FIND_PLAYERS, không có filter Level. StartTo
phải trong tương lai và không vượt cửa sổ tối đa cấu hình tính từ hiện tại.
ACTIVE tự hết hạn khi đến StartTo; PAUSED cũng hết hạn. Sửa watch đã hết hạn
chuyển thành PAUSED, cần activate để chạy lại. Watch CANCELLED không sửa/kích hoạt lại.

## Thông báo và worker

Gửi history snapshot cho bên liên quan khi tạo/duyệt/từ chối/rút yêu cầu,
sửa/hủy kèo, xác nhận/hủy/hoàn tất trận hoặc hết hạn. Team nhận theo thành viên
hoặc quản lý hiện tại tùy action; không gửi trùng một user trong một action.
Thông báo watch có ListingId, StartAt, AddressText, DistanceMeters và metadata
SchemaVersion=1 để web/mobile điều hướng; HTML được encode từ text.

Tạo/sửa kèo, rút chỗ đã duyệt, tạo/sửa/kích hoạt watch ghi matching outbox cùng
transaction với thay đổi. Worker backfill kèo hiện có cho watch mới và quét
reconciliation mỗi phút để phục hồi trường hợp thiếu event. Các lượt dùng batch
100, cursor theo UUID lưu vào payload cùng commit, tiếp tục được sau restart.
Worker dùng khóa advisory theo transaction để tuần tự hóa giữa các host; lấy
outbox bằng FOR UPDATE SKIP LOCKED. Trước khi tạo history, khóa listing/watch,
kiểm tra lại trạng thái, thời gian, phạm vi và bên đăng ký. Publish đang chạy
có thể commit trước pause/cancel; sau khi pause/cancel commit không tạo lượt mới.

listing_watch_deliveries có khóa duy nhất UserId × ListingId, giữ dedup kể cả khi
history/outbox đã được cleanup. Nhiều watch trùng nhau chỉ có một thông báo cho
user/kèo; nếu có watch đang hoạt động khớp và bật email thì gộp lựa chọn email.
Bật lại/sửa watch không gửi lại kèo đã thông báo. Nội dung là snapshot lúc tạo,
kèo có thể thay đổi sau đó; client tải lại chi tiết trước khi đăng ký.

History độc lập NotificationConfigId=null, aggregate LISTING/MATCH. SMTP do
NotificationJobs xử lý riêng; matching worker không gửi provider trong transaction
HTTP. Retry dùng SystemSettings và mã lỗi an toàn, rollback toàn batch lỗi;
không ghi thành công giả. Email chưa xác minh được SKIPPED; kênh chưa cấu hình
trả NOTIFICATION_CHANNEL_UNAVAILABLE khi yêu cầu email. SMTP vẫn có giới hạn
at-least-once như mô tả tại [notifications](notifications.md).

- Matching:WorkersEnabled mặc định true; worker chỉ đăng ký ở API.
- Matching:Notifications:EmailEnabled mặc định false, bật email action khi SMTP đã cấu hình.
- Watch emailEnabled độc lập với cờ email action; cũng yêu cầu SMTP hợp lệ.
- Notifications:WorkersEnabled cần true để xử lý email đã enqueue.
- Các secret SMTP giữ trong environment/user-secrets hiện có, không lưu vào watch.

## Cấu hình, migration và kiểm thử

SystemSettings bổ sung MaxActiveListingWatchesPerUser (20, cho phép 0 để chặn
kích hoạt mới), MaxListingSearchRadiusMeters (50000) và MaxListingWatchWindowDays
(30). Cùng giới hạn bán kính áp dụng cho query/watch. Quota đếm ACTIVE chưa hết
hạn; Serializable bảo vệ hai lệnh tạo/kích hoạt cùng lúc. Giảm giới hạn không tự
xóa watch. PUT system-settings yêu cầu cả ba trường mới cùng các trường hiện có.

Migration AddMatchManagementAndListingWatches tạo hai bảng watch/delivery,
index/check/FK/concurrency/audit và ba cột settings. Backfill geography cho
listing/match cũ còn null bằng tọa độ hiện có; SaveChanges tiếp tục đồng bộ geography.
Không xóa lịch sử request/participant khi hủy. Chạy migrator theo quy trình
[persistence](persistence.md) trước khi chạy API với schema mới.

MatchingDomainTests kiểm tra invariant/chuyển trạng thái. MatchingContractTests
và MatchingAuthorizationTests kiểm tra mapping và permission pipeline.
MatchingPersistenceTests chạy PostgreSQL/PostGIS trong database riêng, bao gồm
vãng lai/đối thủ, quyền sau chuyển đội trưởng, contact, HTTP giả actor, radius/
UTC/level, dedup/email, batch qua restart, rollback/retry, quota, chỗ cuối,
hủy/duyệt đồng thời, hoàn tất, hủy quản trị/audit và tự hết hạn. Chạy từ backend bằng scripts/Test.ps1 để lấy
kết nối container; fixture tự tạo/xóa database test, không dùng dữ liệu app.

## Nhắc lịch trận đấu

SystemSettings.MatchReminderLeadMinutes mặc định 60, hợp lệ 1–10080 phút.
Matching worker chạy batch nhắc cùng chu kỳ 5 giây. Chỉ trận CONFIRMED có
StartAt - khoảng nhắc <= now < StartAt và chưa ReminderPublishedAt được xử lý.
Trận xác nhận muộn trong cửa sổ vẫn được nhắc ở chu kỳ tiếp theo; worker dừng
qua giờ bắt đầu thì bỏ qua, không gửi backlog sau giờ đá. Trận PENDING, CANCELLED,
COMPLETED không được nhắc. Lịch chỉ được sửa khi PENDING nên không có lịch nhắc
cũ của trận CONFIRMED cần tái lập.

Khóa cả listing/match và transaction Serializable giữ dấu ReminderPublishedAt,
history snapshot và email outbox trong cùng commit. Rollback không đánh dấu đã nhắc.
Dấu trên match tồn tại khi history cleanup; nhiều host hoặc đổi setting không tạo lại
lượt đã commit. Worker dùng actor null, token của match có thể đổi khi xuất bản nhắc;
client cần tải lại token trước lệnh ghi.

Người nhận là cá nhân đang tham gia và thành viên ACTIVE của các team tham gia
tại thời điểm nhắc, cộng quản lý đội chủ nhà; user không hoạt động/đã xoá được lọc.
Một user nhận một history dù xuất hiện nhiều nguồn. In-app luôn được tạo cho người
nhận hợp lệ; email theo Matching:Notifications:EmailEnabled và SMTP đã cấu hình.
Loại MATCH_REMINDER dùng aggregate MATCH, metadata VIEW_ENTITY với MatchId/ListingId,
StartAt và địa điểm. Email nhắc hết hạn tại StartAt; dispatcher khoá match và bỏ qua
nếu đã huỷ/hoàn tất trước khi gửi. In-app đã xuất bản vẫn là snapshot lịch sử,
client tải lại trận khi mở. SMTP giữ giới hạn at-least-once.

Migration AddMatchReminders thêm setting, reminder_published_at và partial index;
không sửa các migration trước. Chạy migrator trước khi chạy API với schema mới.
