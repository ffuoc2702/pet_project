# MatchingSports coding agent

## Mục tiêu

Bạn là coding agent của MatchingSports. Viết code rõ nghĩa, dễ kiểm thử và tuân thủ
Clean Architecture. Dùng luồng update user profile làm convention chuẩn cho các
use case ghi mới. Khi README cũ khác với hướng dẫn này, đối chiếu code hiện tại và
ưu tiên các quy tắc kiến trúc trong file này; không sao chép lỗi có sẵn.


## Ranh giới các tầng

- **Domain**: entity, value object, invariant, quy tắc nghiệp vụ và chuyển trạng thái.
  Quy tắc phải đúng dù được gọi từ API, worker hay một use case khác.
- **Application**: command/query, handler, DTO và abstraction. Điều phối tải dữ liệu,
  kiểm tra quyền use case, gọi hành vi Domain, lưu transaction và trả kết quả.
- **Infrastructure**: triển khai repository, unit of work, EF Core, migration,
  email, token và OAuth provider. Quyết định kỹ thuật persistence nằm ở đây.
- **Api**: HTTP contract, validation đầu vào HTTP, authentication/claims,
  gửi MediatR request và đóng gói response. Composition root được đăng ký
  Infrastructure; controller không truy vấn DbContext hoặc repository trực tiếp.
- **Shared**: constants và tiện ích thuần dùng chung, ví dụ `ErrorCodes` và
  `StringNormalizer`. Không chứa nghiệp vụ, entity, DTO feature hoặc I/O.

Dependency hướng vào trong:

```text
Domain         -> Shared
Application    -> Domain, Shared
Infrastructure -> Application, Domain, Shared
Api            -> Application, Infrastructure, Shared
Shared         -> không tham chiếu các project MatchingSports khác
```

Không thêm dependency web/ORM/provider vào Domain. Hiện Domain có dependency
ASP.NET Core Identity cho model User; đây là hiện trạng cần được nhận biết, không
phải mẫu để thêm framework vào entity nghiệp vụ mới. Không refactor dependency
này ngoài phạm vi công việc được giao.

## Convention chuẩn: update user profile

Đọc các file sau trước khi xây use case ghi tương tự:

- `backend/src/MatchingSports.Api/Controllers/AuthController.cs`
- `backend/src/MatchingSports.Api/Contracts/UserProfile/UpdateUserProfileRequest.cs`
- `backend/src/MatchingSports.Application/Features/UserProfile/Update/UpdateUserProfileCommand.cs`
- `backend/src/MatchingSports.Application/Abstractions/UserProfile/IUserProfileRepository.cs`
- `backend/src/MatchingSports.Domain/Entities/UserProfile.cs`
- `backend/src/MatchingSports.Infrastructure/Persistence/Repositories/UserProfileRepository.cs`
- `backend/tests/MatchingSports.Tests/UpdateUserProfileTests.cs`

Luồng chuẩn:

```text
HTTP request + authenticated user ID
  -> Controller: tạo command, mediator.Send(command, cancellationToken)
  -> Handler: repository.GetForUpdateAsync(id, cancellationToken)
  -> Handler: xử lý không tìm thấy / quyền truy cập
  -> Domain: entity.UpdateDetails(...) hoặc hành vi nghiệp vụ tương ứng
  -> Handler: unitOfWork.SaveChangesAsync(cancellationToken)
  -> Handler: map entity thành DTO
  -> Controller: OkResponse(result)
```

Command user profile lấy UserId từ claims đã xác thực, không từ request body.
`GetForUpdateAsync` trả entity tracked; đây không phải lời hứa khóa hàng database.
Query chỉ đọc nên dùng projection hoặc `AsNoTracking()` trong Infrastructure.
Repository không tự gọi SaveChanges; handler điều phối commit qua `IUnitOfWork`.

### Đặt nghiệp vụ trong Domain

- Thay đổi entity bằng phương thức thể hiện ý định: `UpdateDetails`, `AcceptRequest`,
  `CancelMatch`, `DeactivateSport`. Handler không gán từng property để thực hiện
  một thay đổi nghiệp vụ.
- Chuẩn hóa dữ liệu của entity trong phương thức Domain. Theo UserProfile:
  DisplayName bắt buộc, trim hai đầu; chuỗi tùy chọn dùng
  `StringNormalizer.StartEndSpaceTrim`, rỗng/whitespace thành null.
- Kiểm tra invariant trước khi thay đổi trạng thái để tránh cập nhật một phần rồi
  mới phát hiện lỗi. Validation ở API không thay thế invariant của Domain.
- Quy tắc liên quan nhiều entity có thể nằm trong domain service thuần; Application
  tải dữ liệu cần thiết và truyền vào. Domain không gọi database hay external API.
- Với entity mới, ưu tiên private setter và factory/phương thức nghiệp vụ phù hợp
  EF Core. Entity hiện có public setter không cho phép bỏ qua hành vi Domain trong
  use case mới; không đổi hàng loạt setter ngoài phạm vi task.
- Không đưa HTTP status, IActionResult, MediatR request hay Application exception
  vào Domain. Dùng guard/domain exception phù hợp và ánh xạ lỗi ở tầng ngoài.

### Controller và handler

- Request API dùng DataAnnotations theo convention hiện có; command dùng
  `sealed record ... : IRequest<TResult>`; handler dùng `IRequestHandler<...>`.
- Controller chỉ xử lý giao thức. Handler chỉ điều phối use case, không chứa
  business rule hoặc thao tác chuẩn hóa entity thay cho Domain.
- Truyền CancellationToken xuyên suốt các lời gọi async hỗ trợ cancellation.
- Dùng DTO để trả kết quả; không trả entity EF/navigation ra API.
- Entity -> DTO dùng ApplicationMappingProfile; DTO -> response riêng dùng
  ContractMappingProfile khi cần. Handler đã trả DTO thì controller không map lại
  cùng kiểu chỉ để sao chép dữ liệu.
- Success dùng `BaseResponse<T>`; danh sách phân trang dùng convention
  `PaginatedResponse<T>` hiện có. Khai báo response type đúng payload thực tế.
- Dùng exception handling tập trung và Problem Details. Không catch chỉ để log
  rồi throw lại; tránh log cùng lỗi ở nhiều tầng. Catch chỉ khi có recovery,
  chuyển lỗi kỹ thuật hoặc cleanup thực sự cần thiết.
- Mã lỗi ổn định dùng `MatchingSports.Shared.ErrorCodes`; không hard-code chuỗi
  như `"INVALID_ACCESS_TOKEN"`. Constants dùng chung có tên rõ nghĩa và được
  nhóm theo mục đích; không biến mọi literal cục bộ thành constant.

## Tên và tổ chức code

- Tên phải diễn đạt hành động, đối tượng và mục đích. Tránh `Process`, `DoWork`,
  `ExecuteLogic`, `Helper`, `Manager` khi tên không cho biết hành vi cụ thể.
- Ví dụ: `GetForUpdateAsync`, `UpdateUserProfile`, `UpdateDetails`,
  `SendEmailVerificationOtpAsync`, `RevokeRefreshSessionAsync`.
- Phương thức async tự định nghĩa có hậu tố Async. Giữ tên bắt buộc của framework
  như MediatR `Handle` và theo convention controller hiện có.
- PascalCase cho type/member, camelCase cho parameter/local. Boolean thể hiện
  điều kiện như `IsActive`, `HasAvailableCapacity`, `CanAcceptRequest`.
- Không đổi `StringNormalizer.StartEndSpaceTrim` hoặc các public API đang dùng
  chỉ vì sở thích đặt tên; rename phải có lý do và cập nhật toàn bộ call site.
- File và thư mục theo feature hiện tại:

```text
Api/Contracts/<Feature>/<Action>Request.cs
Application/Features/<Feature>/<Action>/<Action>Command.cs
Application/Features/<Feature>/Dtos/<Feature>Dto.cs
Application/Abstractions/<Feature>/I<Feature>Repository.cs
Domain/Entities/<Entity>.cs
Infrastructure/Persistence/Repositories/<Feature>Repository.cs
```

- Namespace theo file lân cận: `MatchingSports.Api.Contracts`,
  `MatchingSports.Application.Features.<Feature>`,
  `MatchingSports.Application.Abstractions`, `MatchingSports.Domain.Entities`,
  `MatchingSports.Infrastructure.Persistence.Repositories`, `MatchingSports.Shared`.
- Command và handler có thể cùng file theo mẫu UpdateUserProfile hiện tại.
  Tách khi kích thước/độ phức tạp cần thiết; không tái tổ chức toàn repo cho task nhỏ.
- Dùng nullable đúng nghĩa, guard clause, hàm ngắn với một trách nhiệm. Comment
  giải thích lý do hoặc ràng buộc khó thấy; không diễn giải lại code rõ nghĩa.
- Không thêm generic repository, base handler hay abstraction mới nếu chưa có
  nhu cầu cụ thể. Tái sử dụng abstraction hiện có trước.

## Persistence, bảo mật và kiểm thử

- EF configuration và migration nằm ở Infrastructure. Không sửa migration đã
  triển khai; tạo migration mới khi schema thay đổi.
- Giữ convention concurrency token hiện có khi cập nhật entity có cạnh tranh ghi.
  Không tự ghi đè dữ liệu khi gặp conflict.
- Secret nằm trong user-secrets/environment; không commit token/password/OTP.
  Không log password, OTP, access token, refresh token hoặc dữ liệu riêng tư đầy đủ.
- Test rule/normalization trực tiếp ở Domain khi thêm hoặc thay đổi nghiệp vụ.
  Test handler cho điều phối, not found, authorization và số lần commit khi cần.
  Dùng UpdateUserProfileTests làm mẫu stub repository/unit of work.
- Với thay đổi ranh giới tầng, chạy ArchitectureTests. Với API/persistence, kiểm
  tra contract và integration phù hợp mức rủi ro. Không tạo test chỉ lặp lại code.

Chạy từ `backend/` khi sửa code backend:

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
