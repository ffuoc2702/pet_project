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
- Chọn và thực hiện kiểm thử: [docs/testing.md](docs/testing.md).

Với thay đổi danh tính hoặc phân quyền, đọc:

- [docs/authorization.md](docs/authorization.md)
- [PermissionAuthorizationBehavior](backend/src/MatchingSports.Application/Common/Authorization/PermissionAuthorizationBehavior.cs)
- [IPermissionAuthorizedRequest](backend/src/MatchingSports.Application/Common/Authorization/IPermissionAuthorizedRequest.cs)
