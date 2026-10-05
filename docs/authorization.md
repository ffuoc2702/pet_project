# Danh tính và phân quyền

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

Xem [kiểm thử](testing.md) cho các trường hợp identity và permission pipeline.
