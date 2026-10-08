# MatchingSports coding agent

## Mục tiêu

Viết code rõ nghĩa, dễ kiểm thử và tuân thủ Clean Architecture.
Chọn use case hiện có phù hợp cùng loại thay đổi. Với use case ghi tiêu chuẩn
có người dùng đã xác thực, UpdateUserProfile là implementation tham chiếu hiện tại;
convention không phụ thuộc vào riêng feature này.
Khi README cũ khác với hướng dẫn này, đối chiếu code hiện tại và ưu tiên các
quy tắc kiến trúc trong AGENTS.md và tài liệu được liên kết; không sao chép lỗi có sẵn.

## Ranh giới các tầng

- **Domain**: entity, value object, invariant, quy tắc nghiệp vụ và chuyển trạng thái.
  Quy tắc phải đúng dù được gọi từ API, worker hay use case khác; không gọi I/O.
- **Application**: command/query, handler, DTO và abstraction. Điều phối tải dữ liệu,
  kiểm tra quyền use case, gọi hành vi Domain, lưu transaction và trả kết quả.
- **Infrastructure**: repository, unit of work, EF Core, migration và external services.
  Quyết định kỹ thuật persistence nằm ở đây.
- **Api**: HTTP contract, validation đầu vào HTTP, authentication/claims,
  gửi MediatR request và đóng gói response. Composition root đăng ký Infrastructure;
  controller không truy vấn DbContext hoặc repository trực tiếp.
- **Shared**: constants và tiện ích thuần dùng chung; không chứa nghiệp vụ,
  entity, DTO feature hoặc I/O.

## Chiều dependency

```text
Domain         -> Shared
Application    -> Domain, Shared
Infrastructure -> Application, Domain, Shared
Api            -> Application, Infrastructure, Shared
Shared         -> không tham chiếu các project MatchingSports khác
```

Không thêm dependency web/ORM/provider vào Domain. Dependency ASP.NET Core Identity
hiện có cho model User không phải mẫu cho entity mới; không refactor ngoài phạm vi task.

## Golden path tổng quát

```text
HTTP contract + danh tính đã xác thực
  -> Controller: tạo command/query, gửi qua MediatR với CancellationToken
  -> Pipeline: kiểm tra permission/validation được khai báo trên request
  -> Handler: tải dữ liệu, xử lý not found và quyền phụ thuộc dữ liệu
  -> Domain: kiểm tra invariant, thực hiện hành vi nghiệp vụ khi ghi
  -> Handler: commit qua IUnitOfWork khi ghi, map kết quả thành DTO
  -> Controller: đóng gói response theo contract
```

Query chỉ đọc không commit. Repository không tự gọi SaveChanges.
Danh tính người thực hiện lấy từ nguồn tin cậy, không lấy từ HTTP body;
phân biệt actor với người dùng mục tiêu. Identity không thay thế permission.

## Entity base, audit và concurrency

- Entity nghiệp vụ có audit kế thừa `AuditableEntity`, gồm `CreatedAt`, `UpdatedAt`,
  `CreatedBy`, `UpdatedBy`. Entity cần optimistic concurrency kế thừa
  `ConcurrentAuditableEntity`; base này kế thừa `AuditableEntity` và triển khai
  `IHasConcurrencyToken`. Không khai báo lại các property đã kế thừa.
- Không đưa `Id` vào base: entity có thể có khóa đơn hoặc khóa ghép. Base nằm trong
  Domain, không đọc claims, DbContext hoặc gọi I/O. EF configuration ở Infrastructure
  giữ các entity thành bảng độc lập; không tạo bảng base hoặc discriminator.
- `User` và `Role` giữ base Identity, triển khai `IAuditableEntity` với property audit
  riêng. Giữ concurrency stamp của Identity; không thay bằng GUID token của base.
  `AdminAction` là lịch sử bất biến, giữ cấu trúc riêng; các bảng liên kết thuần
  `RolePermission`, `UserSportProfileFormat` và Identity giữ cấu trúc hiện có.
- Metadata audit có private setter, không nhận từ HTTP body. Infrastructure stamp
  audit cho cả SaveChanges sync/async: tạo ghi cả hai timestamp và actor; sửa giữ
  nguyên `CreatedAt`/`CreatedBy`, chỉ cập nhật `UpdatedAt`/`UpdatedBy`.
- `IAuditActorContext` có lifetime scoped; `BeginScope(actorUserId)` phải được dispose
  bằng `using`, hỗ trợ scope lồng nhau và tách actor giữa các luồng async. Không dùng
  singleton hoặc suy luận actor từ owner, recipient, user mục tiêu hay ID trong body.
- Request có `IPermissionAuthorizedRequest` được `AuditActorBehavior` thiết lập actor
  sau khi permission pipeline cho phép. Use case của chính mình không có interface
  này lấy ID qua `ICurrentUser` và mở scope audit trong handler trước khi ghi.
  Không thay permission bằng audit context; khi gọi qua MediatR không lặp scope chỉ
  để lấy lại cùng actor đã được behavior thiết lập.
- Luồng Identity anonymous dùng actor null cho đăng ký và các thao tác chưa chứng
  minh danh tính. Chỉ mở scope user sau khi kiểm tra password, OTP, refresh token
  hoặc xác thực external provider thành công. Email/user ID mục tiêu chưa đủ làm actor.
- Worker/seed/dữ liệu cũ không xác định được actor dùng null; không dùng `Guid.Empty`
  hoặc tự gán người nhận làm người tạo. Caller nền có actor tin cậy mở scope rõ ràng.
  Các cột actor là UUID lịch sử, không có FK cascade làm mất dấu vết khi xóa user.
- GUID concurrency token vẫn cần `.IsConcurrencyToken()` ở Infrastructure và được
  xoay khi thêm/sửa. Domain dùng `RenewConcurrencyToken()` nếu cần một cập nhật
  không đổi nội dung vẫn tham gia kiểm tra concurrency. Không ghi đè khi conflict.
- Resource DTO trả metadata audit sau commit; không nhận audit trong request.
  DTO audit kế thừa `BaseDto`; DTO dùng GUID concurrency kế thừa
  `ConcurrentBaseDto : BaseDto`. Các base và DTO kế thừa dùng `record`, không
  khai báo lại audit/token trong constructor hoặc property của DTO feature.
  `RoleDto` giữ `ConcurrencyStamp` của Identity, không dùng GUID concurrency base.
  Snapshot `AdminAction` mô tả thay đổi nghiệp vụ, không chụp metadata audit chưa
  được persistence stamp. Ghi lịch sử và thay đổi nghiệp vụ trong cùng transaction.

## Tính năng thông báo

- Đọc [docs/notifications.md](docs/notifications.md) trước khi sửa notification,
  lịch, kênh gửi, outbox delivery hoặc cleanup. `NotificationHistory` thay thế
  `Notification`; một history là một user × một lượt, nội dung là snapshot.
- `NotificationConfig` dùng concurrency base; DTO dùng base tương ứng. User/team
  mục tiêu nằm trong junction, không phải actor. API hộp thông báo chỉ đọc history
  của chính mình; không nhận `AdminView` hoặc actor từ HTTP.
- Domain giữ invariant và chuyển trạng thái; Application `NotificationJobs` điều
  phối. Infrastructure giữ cron, HTML sanitizer, SMTP, lock và transaction.
- Claim bằng lease/token và khóa hàng; history/outbox/counter/lịch commit cùng
  transaction. Retry outbox không tăng `SendCount` hoặc `SendNumber`.
- Metadata HTTP là JSON object với `SchemaVersion`; HTML phải được làm sạch,
  `BodyText` và điều hướng dùng chung web/mobile. `ReadAt` độc lập delivery status.
  Khóa metadata chuẩn nhận camelCase/PascalCase, chuẩn hóa thành PascalCase;
  từ chối khóa trùng nghĩa để tránh diễn giải khác nhau giữa các client.
- Phase hiện tại hỗ trợ in-app/email. Push/SMS không được kích hoạt trước khi có
  adapter; thiết bị/token thuộc phase mobile sau. Không ghi nhận gửi thành công giả.
- `ExpiredAfter` dừng gửi; `PurgeAfter` quản lý retention. Cleanup không xóa history
  còn delivery pending. Worker đăng ký ở API, không ở migrator; không log nội dung,
  địa chỉ nhận, token hoặc lỗi thô từ provider.

## Quy tắc code cốt lõi

- Business rule, normalization và chuyển trạng thái thuộc Domain; kiểm tra invariant
  trước khi thay đổi trạng thái. Validation ở API không thay thế invariant Domain.
- Thay đổi entity qua phương thức thể hiện ý định; handler chỉ điều phối use case.
- Truyền CancellationToken xuyên suốt lời gọi async hỗ trợ cancellation.
- Trả DTO, không trả entity EF/navigation ra API; không map lại DTO cùng kiểu.
- Mọi API list độc lập phải phân trang; đọc tài liệu phân trang và query trước khi sửa.
- Dùng exception handling tập trung và Problem Details; không catch chỉ để log rồi throw lại.
  Mã lỗi ổn định dùng MatchingSports.Shared.ErrorCodes.
- Tên diễn đạt hành động, đối tượng và mục đích; PascalCase cho type/member,
  camelCase cho parameter/local, hậu tố Async cho phương thức async tự định nghĩa.
  Giữ tên bắt buộc của framework và convention controller hiện có.
- Theo cấu trúc feature và namespace lân cận. Dùng nullable đúng nghĩa, guard clause,
  hàm ngắn với một trách nhiệm; comment giải thích lý do hoặc ràng buộc khó thấy.
- Tái sử dụng abstraction hiện có; không thêm generic repository/base handler
  khi chưa có nhu cầu cụ thể. Không đổi public API chỉ vì sở thích đặt tên.
- Secret nằm trong user-secrets/environment; không commit hoặc log password, OTP,
  token hay dữ liệu riêng tư đầy đủ.

## Lệnh kiểm tra

Chạy từ backend/ khi sửa code backend:

```powershell
dotnet restore .\MatchingSports.slnx --locked-mode
dotnet build .\MatchingSports.slnx --configuration Release --no-restore
dotnet test .\MatchingSports.slnx --configuration Release --no-build --no-restore
dotnet format .\MatchingSports.slnx --verify-no-changes --no-restore
```

Chỉ cập nhật package/lock file khi task cần. Nếu không chạy được kiểm tra, nêu
chính xác nguyên nhân; không báo đã pass khi chưa chạy.

## Cách thực hiện một task

1. Đọc hướng dẫn và code use case tương tự; kiểm tra thay đổi sẵn có của người dùng.
2. Xác định rule thuộc Domain, orchestration thuộc Application và I/O thuộc
   Infrastructure trước khi triển khai.
3. Thực hiện thay đổi nhỏ nhất đáp ứng yêu cầu; giữ thay đổi không liên quan.
4. Rà lại tên hàm, chiều dependency, normalization, authorization, mã lỗi,
   mapping và transaction; kiểm thử phù hợp thay đổi.
5. Bàn giao ngắn gọn: hành vi đã thay đổi, file chính, kết quả kiểm tra và vấn đề
   còn tồn tại. Khi task chỉ review/giải thích, không tự sửa code.

## Tài liệu chi tiết theo phạm vi task

Đọc tài liệu tương ứng và code tham chiếu trước khi triển khai:

- Thay đổi kiến trúc hoặc nghiệp vụ Domain: [docs/architecture.md](docs/architecture.md).
- Thêm/sửa use case ghi: [docs/conventions/commands.md](docs/conventions/commands.md).
- Thêm/sửa query, mapping hoặc filter: [docs/conventions/queries.md](docs/conventions/queries.md).
- Thêm/sửa API list: [docs/pagination.md](docs/pagination.md).
- Thay đổi persistence, schema hoặc concurrency: [docs/persistence.md](docs/persistence.md).
- Entity base và metadata audit: [docs/auditing.md](docs/auditing.md).
- Notification, scheduling, delivery và mobile contract: [docs/notifications.md](docs/notifications.md).
- Chọn và thực hiện kiểm thử: [docs/testing.md](docs/testing.md).

Với thay đổi danh tính hoặc phân quyền, đọc:

- [docs/authorization.md](docs/authorization.md)
- [PermissionAuthorizationBehavior](backend/src/MatchingSports.Application/Common/Authorization/PermissionAuthorizationBehavior.cs)
- [IPermissionAuthorizedRequest](backend/src/MatchingSports.Application/Common/Authorization/IPermissionAuthorizedRequest.cs)

## Các kĩ năng

Đọc [SKILL.md](./SKILL.md)