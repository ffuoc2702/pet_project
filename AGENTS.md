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
  -> MediatR pipeline: kiểm tra permission nếu request triển khai IPermissionAuthorizedRequest
  -> Handler: lấy user ID qua ICurrentUser.UserId
  -> Handler: repository.GetForUpdateAsync(userId, cancellationToken)
  -> Handler: xử lý không tìm thấy / quyền truy cập
  -> Domain: entity.UpdateDetails(...) hoặc hành vi nghiệp vụ tương ứng
  -> Handler: unitOfWork.SaveChangesAsync(cancellationToken)
  -> Handler: map entity thành DTO
  -> Controller: OkResponse(result)
```

Command user profile chỉ chứa dữ liệu cần cập nhật, không chứa UserId.
Handler lấy UserId qua ICurrentUser; implementation ở API đọc claims đã xác thực.
Không lấy danh tính người thực hiện từ request body.
`GetForUpdateAsync` trả entity tracked; đây không phải lời hứa khóa hàng database.
Query chỉ đọc nên dùng projection hoặc `AsNoTracking()` trong Infrastructure.
Repository không tự gọi SaveChanges; handler điều phối commit qua `IUnitOfWork`.

### Danh tính người thực hiện: ICurrentUser

- Abstraction nằm tại
  `backend/src/MatchingSports.Application/Abstractions/Identity/ICurrentUser.cs`,
  namespace `MatchingSports.Application.Abstractions`, cung cấp `Guid UserId { get; }`.
- Implementation `CurrentUser` nằm trong `Api/Identity`, dùng `IHttpContextAccessor`.
  Application không đọc HttpContext, ClaimsPrincipal hoặc phụ thuộc ASP.NET Core.
- API đăng ký qua `AddCurrentUser()`: `AddHttpContextAccessor()` và
  `AddScoped<ICurrentUser, CurrentUser>()`. Không giữ user ID trong singleton.
- `CurrentUser` yêu cầu identity đã xác thực và claim `ClaimTypes.NameIdentifier`
  parse được Guid. Thiếu context, identity chưa xác thực hoặc claim thiếu/sai thì
  ném `UnauthorizedException` với `ErrorCodes.InvalidAccessToken`.
- Use case thao tác hồ sơ của chính mình lấy ID trong handler qua `ICurrentUser`.
  Các request quản trị hiện có giữ `ActorUserId`; controller lấy giá trị này qua
  `ICurrentUser.UserId`, không tự viết lại `GetActorUserId()` hoặc parse claims.
- Phân biệt `ActorUserId` (người thực hiện) với `UserId` (người dùng mục tiêu).
  Không dùng ID mục tiêu để kiểm tra quyền của actor. Caller ngoài HTTP phải cung
  cấp danh tính actor từ nguồn tin cậy; không giả định có HttpContext.
- `ICurrentUser` xác định danh tính, không thay thế kiểm tra permission.

### Phân quyền chung: MediatR pipeline

Đọc các file sau khi thêm hoặc sửa use case có kiểm tra permission:

- `backend/src/MatchingSports.Application/Common/Authorization/IPermissionAuthorizedRequest.cs`
- `backend/src/MatchingSports.Application/Common/Authorization/PermissionAuthorizationBehavior.cs`
- `backend/src/MatchingSports.Application/DependencyInjection.cs`
- `backend/src/MatchingSports.Application/Features/Role/GetRoles/GetRolesQuery.cs`
- `backend/tests/MatchingSports.Tests/PermissionAuthorizationBehaviorTests.cs`

Quy tắc:

- Command/query cần kiểm tra permission chung triển khai
  `IPermissionAuthorizedRequest`, khai báo `ActorUserId` và `RequiredPermissions`.
  Danh sách quyền cố định theo use case, dùng constants `PermissionCodes`; không
  lấy danh sách quyền yêu cầu từ HTTP body. Ưu tiên explicit interface implementation
  cho `RequiredPermissions` theo `GetRolesQuery`.
- `PermissionAuthorizationBehavior<,>` được đăng ký bằng `AddOpenBehavior` trong
  `AddApplication()`, trước các behavior validation. Behavior chỉ áp dụng cho
  request triển khai `IPermissionAuthorizedRequest`.
- Pipeline tải quyền actor qua `IUserRoleRepository.GetUserRolesAsync` một lần
  cho mỗi request được kiểm tra và yêu cầu đủ **tất cả** permission đã khai báo.
  Actor không tồn tại hoặc thiếu quyền thì ném `ForbiddenException` với
  `ErrorCodes.Forbidden`; không gọi handler. Danh sách quyền rỗng hoặc chứa mã
  whitespace là lỗi khai báo, không được dùng để bỏ qua phân quyền.
- Gửi các request này qua `IMediator.Send(..., cancellationToken)`, kể cả khi gọi
  từ worker hoặc use case khác. Gọi trực tiếp `handler.Handle` sẽ bỏ qua pipeline.
- Không lặp lại `RequirePermissionAsync`, `RequirePermissionsAsync` hoặc tải quyền
  actor chỉ để kiểm tra cùng permission trong handler. Handler giữ các kiểm tra
  phụ thuộc dữ liệu như quyền trên bản ghi cụ thể và concurrency; invariant vẫn
  thuộc Domain. Pipeline không thay thế các kiểm tra này.
- `[Authorize(Policy = ...)]` bảo vệ endpoint HTTP; pipeline bảo vệ use case khi
  được gọi qua MediatR. Giữ policy HTTP khớp permission của command/query.
  Quyền `list` và `read` độc lập; truy vấn ma trận role yêu cầu cả
  `PermissionCodes.ListRoles` và `PermissionCodes.ListPermissions`.
- Request không triển khai interface không được pipeline này kiểm tra permission.
  Không suy luận rằng mọi use case đã được bảo vệ chỉ vì đã đăng ký behavior.

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

### Phân trang bắt buộc cho API list

- Mọi API list/collection phải nhận `PageNumber` và `PageSize` qua query string,
  kể cả danh sách dùng cho dropdown hoặc màn hình quản trị. Không thêm chế độ
  trả toàn bộ dữ liệu hoặc phân trang sau khi đã tải toàn bộ danh sách.
- Request HTTP kế thừa `BaseRequest` (kế thừa `PaginatedRequest`); mặc định trang 1, kích thước 20,
  tối đa 100 theo `PaginationDefaults`. Dùng DataAnnotations ở API và
  `IPaginatedQuery` với `PaginationValidationBehavior` trong Application để
  bảo vệ cả caller ngoài HTTP; từ chối trang/kích thước không hợp lệ và offset
  vượt `int.MaxValue` bằng `ErrorCodes.InvalidPagination`.
- Query list trả `PaginatedResult<TDto>`. Handler chuyển thông số phân trang
  xuống repository và map bằng `PaginatedResult.Map` để giữ nguyên metadata.
  Controller dùng `OkPaginatedResponse` và khai báo
  `PaginatedResponse<T>` đúng kiểu payload; không map lại DTO cùng kiểu.
- Infrastructure áp dụng filter trước `LongCountAsync`, dùng cùng filter cho
  dữ liệu trang, sắp xếp ổn định với khóa duy nhất (ví dụ `ThenBy(Id)`) trước
  `Skip`/`Take`, và dùng projection hoặc `AsNoTracking()`. Truyền
  `CancellationToken` cho cả count và truy vấn trang; không commit query đọc.
- Metadata gồm `PageNumber`, `PageSize`, `TotalItems`, `TotalPages`,
  `HasPreviousPage`, `HasNextPage`. Trang vượt tổng số trang trả danh sách
  rỗng cùng tổng số mục thực tế, không tự đổi số trang được yêu cầu.
- Ma trận role phân trang theo hàng role: `RolePermissionMatrixDto.Roles`
  là `PaginatedResult<RoleDto>`; `Permissions` giữ đầy đủ cột (kể cả quyền
  chưa được gán), response dùng `BaseResponse<RolePermissionMatrixDto>`.
  Collection thuộc DTO chi tiết như quyền của một role hoặc role của một user
  là dữ liệu của bản ghi đó, không phải API list độc lập. Repository nội bộ
  phục vụ authorization hoặc tải tập ID/code cụ thể không bị cắt trang.
- Khi thêm/sửa API list, kiểm thử trang mặc định, trang kế tiếp, trang rỗng,
  metadata, validation qua MediatR, quyền và cancellation phù hợp mức rủi ro.
  Đọc `SportController`, `GetSportQuery`, `SportRepository` và
  `PaginationTests` làm convention hiện có.

### BaseQuery và mapping request → query

- Mọi query list dùng `sealed record` kế thừa `BaseQuery<TItem>`; request HTTP
  kế thừa `BaseRequest`. `PageNumber`, `PageSize`, `SearchString`,
  `FromDate` và `ToDate` được khai báo ở base, không lặp lại trong constructor
  hoặc query feature. Query chi tiết trả một bản ghi giữ `IRequest<TDto>` riêng.
- Với payload ghép như ma trận, kế thừa `BaseQuery` không generic và khai báo
  đúng `IRequest<RolePermissionMatrixDto>`; không kế thừa hai kiểu MediatR
  response khác nhau. `BaseQuery<TItem>` trả `PaginatedResult<TItem>`.
- Khai báo mapping request → query trong `Api/Contracts/Mapping/ContractMappingProfile.cs`.
  AutoMapper map property trùng tên, kể cả property kế thừa và filter feature.
  Ignore `ActorUserId` trong mapping; controller dùng
  `mapper.Map<GetRolesQuery>(request) with { ActorUserId = currentUser.UserId }`,
  rồi `mediator.Send(query, cancellationToken)`. Không gán thủ công từng field
  phân trang/tìm kiếm và không đưa HTTP request/AutoMapper contract vào Application.
- Actor không nằm trong request HTTP; quyền yêu cầu vẫn cố định trên query.
  Caller ngoài HTTP tạo query bằng object initializer với danh tính tin cậy;
  không có HTTP context hoặc tự động đọc query string trong handler/pipeline.
- Repository list nhận query/filter object từ Application cùng CancellationToken,
  áp dụng search trước count và paging. Search whitespace tương đương không lọc;
  sports tìm theo code/name, roles và hàng ma trận theo name/description,
  permissions theo code/name, không phân biệt hoa thường.
- Filter ngày dùng `UpdatedAt` với hai đầu bao gồm giá trị biên khi entity hỗ trợ.
  Role hiện chưa có timestamp: `GetRolesQuery` và `GetRolePermissionMatrixQuery`
  khai báo `SupportsDateRange = false`; `DateRangeValidationBehavior` từ chối
  ngày được truyền bằng `ErrorCodes.InvalidDateRange`. Không nhận rồi bỏ qua
  filter và không thêm schema ngoài phạm vi task chỉ để đáp ứng base class.
- Khi thêm query list, khai báo mapping và kiểm tra cấu hình AutoMapper, field kế
  thừa/default, filter riêng, actor tin cậy, filter thực tế và metadata sau lọc.

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
  Test handler cho điều phối, not found, quyền phụ thuộc dữ liệu và số lần commit
  khi cần. Dùng UpdateUserProfileTests làm mẫu stub repository/unit of work và
  ICurrentUser.
- Test permission chung đi qua `IMediator.Send` với `AddApplication()` để kiểm tra
  cả DI và pipeline; không gọi trực tiếp handler rồi kỳ vọng pipeline chạy.
  Kiểm tra thiếu actor/permission không gọi handler hoặc commit, đủ tất cả quyền
  mới cho phép, truyền đúng actor ID và CancellationToken, và request không cần
  permission vẫn chạy khi không đăng ký repository phân quyền. Dùng
  PermissionAuthorizationBehaviorTests và ListReadPermissionTests làm mẫu.
- Khi sửa `ICurrentUser`, kiểm tra claims hợp lệ/thiếu/sai, identity chưa xác thực,
  thiếu HttpContext và scoped lifetime theo CurrentUserTests.
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
