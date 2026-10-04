# MatchingSports Backend

Backend .NET 10 theo Clean Architecture, dùng ASP.NET Core, MediatR, AutoMapper,
EF Core, PostgreSQL/PostGIS, Serilog ECS và OpenTelemetry.

## Chạy bằng Visual Studio

Khởi động PostgreSQL/PostGIS local:

```powershell
docker run --name matchingsports-postgres-dev `
  -e POSTGRES_DB=matchingsports `
  -e POSTGRES_USER=postgres `
  -e POSTGRES_PASSWORD=postgres `
  -p 5432:5432 `
  -v matchingsports-postgres-dev-data:/var/lib/postgresql/data `
  -d postgis/postgis:18-3.6
```

Chọn `MatchingSports.Api` làm startup project và chạy profile `https`.
Trong môi trường Development, API tự apply migration và seed reference data.

- Swagger: `https://localhost:7406/swagger`
- Readiness: `https://localhost:7406/health`
- Liveness: `https://localhost:7406/alive`

Hoặc chạy `MatchingSports.AppHost`; AppHost sẽ tạo PostgreSQL, chạy
`MatchingSports.Migrator`, sau đó mới khởi động API và frontend.

## Database production

API production không tự migrate database. Chạy migrator như một deployment job:

```powershell
$env:ConnectionStrings__matchingsports = "Host=...;Database=matchingsports;Username=...;Password=..."
dotnet run --project .\src\MatchingSports.Migrator\MatchingSports.Migrator.csproj --configuration Release
```

Tạo migration mới:

```powershell
dotnet ef migrations add MigrationName `
  --project .\src\MatchingSports.Infrastructure `
  --startup-project .\src\MatchingSports.Infrastructure
```

Seed trong `DatabaseInitializer` là idempotent và chỉ chứa catalog dữ liệu nền.

## API conventions

- Query request kế thừa `BaseRequest`: `PageNumber`, `PageSize`, `SearchString`,
  `FromDate`, `ToDate`.
- Application query kế thừa `BaseQuery<T>` với cùng các field.
- Success dùng `BaseResponse<T>` hoặc `PaginatedResponse<T>`.
- Error dùng RFC Problem Details cùng `code`, `traceId`, `correlationId`.
- Không `try/catch` rồi throw lại trong controller; exception được xử lý tập trung.
- Entity có cạnh tranh ghi dùng `ConcurrencyToken` dạng UUID. Client gửi lại đúng token
  đã nhận; server tự sinh token mới sau mỗi lần ghi và trả `409` nếu token đã cũ.

## Quality checks

```powershell
dotnet restore .\MatchingSports.slnx --locked-mode
dotnet tool restore
dotnet format .\MatchingSports.slnx --verify-no-changes --no-restore
dotnet build .\MatchingSports.slnx --configuration Release --no-restore
dotnet test .\MatchingSports.slnx --configuration Release --no-build --no-restore
dotnet list .\MatchingSports.slnx package --vulnerable --include-transitive
```

Build dùng warnings-as-errors, package lock files và architecture tests.

## Phân quyền RBAC

Role là dữ liệu trong database, không có whitelist ADMIN/USER hay role nào tự
bypass kiểm tra permission. Có thể tạo MODERATOR, SUPPORT, EVENT_ORGANIZER...
và gán bất kỳ tập permission trong catalog cho mỗi role. Một user có nhiều role;
quyền hiệu lực là hợp các permission của những role đang hoạt động. Role bị vô
hiệu hóa vẫn giữ membership và cấu hình để phục hồi, nhưng không cấp quyền.
Không có wildcard, kế thừa role hay deny override ngầm.

`list` và `read` là hai quyền độc lập: `*.list` xem danh sách/tìm kiếm,
`*.read` xem một bản ghi. Có read không tự có list, có list không tự có read;
muốn dùng cả hai cần gán cả hai. Policy HTTP và MediatR pipeline cùng kiểm tra action tương ứng.
Endpoint singleton như `/auth/me` và thông tin liên hệ trận chỉ dùng read,
không tạo quyền list không có ý nghĩa.


Command/query cần kiểm tra permission triển khai `IPermissionAuthorizedRequest`,
khai báo `ActorUserId` và danh sách `RequiredPermissions` cố định.
`PermissionAuthorizationBehavior` tải quyền actor một lần và yêu cầu đủ tất cả
permission trước khi gọi handler. Gửi các request này qua `IMediator.Send`; gọi
trực tiếp `handler.Handle` sẽ bỏ qua pipeline. Handler giữ các kiểm tra phụ thuộc
dữ liệu như concurrency và tính hợp lệ của role; không lặp lại kiểm tra permission chung.
Tên role được Domain trim và chuẩn hóa uppercase; mô tả tùy chọn được trim.
`isDefaultForRegistration` được quản lý qua API role, không qua tên USER:
email registration và Google registration đều nhận các role đang hoạt động có
cờ này. Có thể chọn nhiều role mặc định hoặc không chọn role nào. Đổi cờ chỉ tác
động đăng ký mới; không thay đổi membership của user cũ. Request đăng ký không
được chọn role. Role mặc định không được đồng thời bị vô hiệu hóa.

| API | Permission cần có | Chức năng |
| --- | --- | --- |
| GET /api/v1/role | roles.list | Danh sách role, metadata, phiên bản và permission |
| GET /api/v1/role/{roleId} | roles.read | Chi tiết một role và quyền được gán |
| POST /api/v1/role | roles.create | Tạo role mới, ban đầu không có permission |
| PUT /api/v1/role/{roleId} | roles.update | Đổi tên/mô tả/trạng thái/role mặc định |
| GET /api/v1/role/matrix | roles.list và permissions.list | Catalog cột quyền và tất cả hàng role |
| GET /api/v1/permission | permissions.list | Danh sách permission |
| GET /api/v1/permission/{permissionId} | permissions.read | Chi tiết một permission |
| PUT /api/v1/role/{roleId}/permissions | roles.permissions.update | Thay thế tập permission của một role |
| GET /api/v1/role/users/{userId} | users.read | Đọc membership và phiên bản của một user |
| PUT /api/v1/role/users/{userId} | roles.assign | Thay thế toàn bộ role của user bằng roleIds |
| GET /api/v1/sport | sports.list | Danh sách môn thể thao có phân trang |
| GET /api/v1/sport/{sportId} | sports.read | Chi tiết một môn thể thao |

Các resource collection đã có cặp list/read trong catalog: sports, roles,
permissions, sport_profiles, sport_formats, teams, listings, requests, matches,
notifications, users, audit_logs. Reports có cặp `reports.list_own`/`reports.read_own`
và `reports.list_all`/`reports.read_all` theo phạm vi truy cập hiện có.
Các module chưa có endpoint chỉ được chuẩn bị permission, không tự sinh API.

Ma trận trả `permissions` (mọi cột quyền, kể cả chưa gán) và `roles` (các hàng).
`roles[].permissions` là các ô được cấp; ô không xuất hiện nghĩa là chưa cấp.
Role không hoạt động vẫn có trong ma trận và hiển thị `isActive: false`.
Response membership trả `roleIds` gồm mọi role đang gán, kể cả role tạm vô hiệu
hóa; `roles` là tên các role đang hoạt động. Tra metadata/trạng thái theo ID
trong danh sách role hoặc ma trận.

Tạo role:

```json
{"name":"MODERATOR","description":"Kiểm duyệt nội dung","isDefaultForRegistration":false}
```

Gán quyền cho role; lấy `concurrencyStamp` mới nhất từ GET role/matrix hoặc response
cập nhật trước đó:

```json
{"permissionCodes":["sports.list","sports.read","listings.moderate"],"concurrencyStamp":"<current-role-stamp>"}
```

Gán nhiều role cho user; lấy phiên bản từ GET role/users/{userId}:

```json
{"roleIds":["<role-id-1>","<role-id-2>"],"concurrencyStamp":"<current-user-stamp>"}
```

**Thay đổi contract:** body cũ `{"role":"ADMIN"}` không còn được hỗ trợ.
Cả PUT role, PUT role permissions và PUT user roles đều bắt buộc gửi
`concurrencyStamp`; thiếu trả 400, phiên bản cũ hoặc cạnh tranh ghi trả 409.
Permission/role ID không tồn tại hoặc role gán bị vô hiệu hóa trả 400; thiếu quyền
trả 403; user không tồn tại/không hoạt động hoặc role không tồn tại trả 404.
Tên role trùng không phân biệt hoa/thường trả 409. Danh sách rỗng gỡ toàn bộ grant
hoặc membership; phần tử trùng không tạo bản ghi trùng. Các thay đổi phân quyền
và audit before/after được commit cùng transaction SaveChanges, nên conflict
không để lại audit hoặc grant một phần.

Không chặn theo tên role hay theo việc sửa chính mình: người giữ permission
quản lý có thể sửa cả role/membership của bản thân. Các quyền
`roles.create`, `roles.update`, `roles.assign`, `roles.permissions.update` là quyền
nhạy cảm; chỉ cấp cho tài khoản quản trị được tin cậy. Người thực hiện có thể tự
mất quyền quản trị; cần giữ một tài khoản quản trị dự phòng và quy trình khôi
phục vận hành, không có đường bypass ADMIN ẩn.

JWT, login/refresh và `/api/v1/auth/me` trả `roles` và `permissions`.
Mỗi request bearer tải lại quyền từ database thay cho claim cũ; thu hồi quyền,
đổi membership hoặc vô hiệu hóa role có hiệu lực từ request tiếp theo.
Policy provider tạo policy theo action code của endpoint, không dùng catalog
seed để whitelist quyền runtime. Thêm một endpoint mới vẫn cần triển khai action,
permission constant và seed tương ứng; tạo grant trong database không tự tạo API.
Ownership và business rule vẫn phải được kiểm tra ở use case/Domain.

### Seed và migration

Initializer vẫn seed ADMIN, USER và 67 permission theo
`InitialPermissionCatalog` ở Infrastructure/Persistence/Seed: ADMIN ban đầu có
67 quyền, USER có 41 quyền. Catalog là cấu hình khởi tạo, không phải quy tắc
phân quyền runtime. Permission của module chưa triển khai chỉ là dữ liệu chuẩn bị,
không có nghĩa API tương ứng đã tồn tại.

Seed chạy trong transaction và PostgreSQL advisory lock để tránh seed song song.
Marker khởi tạo bảo đảm chạy lại không khôi phục grant bị gỡ, role mặc định hoặc
membership đã thay đổi. `seed_key` giữ nhận diện role seed khi đổi tên; initializer
không tạo lại ADMIN/USER chỉ vì chúng đã được đổi tên hoặc vô hiệu hóa.
Permission mới bổ sung vào seed được cấp cho các role seed theo cấu hình khởi tạo
một lần khi permission đó được tạo; các lần sau không cấp lại quyền đã thu hồi.
Role tùy chỉnh không nhận quyền từ catalog seed.

Riêng lần nâng cấp tách read/list: khi một mã list lần đầu được tạo, seed sao
chép grant read cũ đang có của mọi role (kể cả role tùy chỉnh) sang grant list
tương ứng để giữ hành vi API cũ. Read đã bị gỡ sẽ không cấp list khi nâng cấp.
Việc sao chép chỉ chạy tại lần tạo mã list, không phải rule runtime; gỡ list
sau đó rồi chạy seed lại không cấp lại list dù role vẫn giữ read.
Tên hiển thị permission seed được cập nhật theo catalog để phân biệt list/read.

Với database cũ, membership chưa có role chỉ được backfill một lần khi nâng cấp.
`Auth:BootstrapAdminEmail` vẫn hỗ trợ bootstrap từ user đã tồn tại, ACTIVE,
đã xác thực email; không tạo tài khoản bằng cấu hình legacy này.

### Seed tài khoản admin ban đầu

Mặc định `Seed:Admin:Enabled` là false; không có mật khẩu admin mặc định.
Bật rõ ràng và cung cấp email, mật khẩu từ environment/user-secrets/secret manager.
Không commit password vào appsettings hoặc truyền password qua command line:

```powershell
$env:Seed__Admin__Enabled = "true"
$env:Seed__Admin__Email = "<admin-email>"
# Seed__Admin__Password phải được inject từ secret manager hoặc environment bảo mật.
# Seed__Admin__DisplayName tùy chọn; mặc định Administrator.
dotnet run --project .\src\MatchingSports.Migrator --configuration Release
```

Seeder tạo một user có profile, ACTIVE, email đã xác thực bằng `UserManager`;
password được hash và phải đạt policy Identity hiện tại: tối thiểu 10 ký tự,
có chữ hoa/chữ thường/số/ký tự đặc biệt. Không gửi OTP/email trong bootstrap.
Role admin seed được tìm theo `seed_key`, không theo tên hiện tại của role.
Role/permission/account/membership và marker hoàn thành seed cùng transaction:
tạo account thất bại thì không để lại dữ liệu seed dở dang.

Nếu email đã tồn tại, chỉ dùng user ACTIVE đã xác thực email; không tự verify,
reactivate, đổi profile, reset password hoặc xóa các role cũ. Tài khoản bị xóa
mềm không được tạo lại. Không dùng bootstrap để chiếm quyền một tài khoản chưa
xác thực. Nếu đã có admin hoặc bootstrap đã hoàn thành thì không tạo admin khác,
không khôi phục role bị gỡ và không reset mật khẩu ở các lần chạy sau.

Sau provisioning: đổi mật khẩu ban đầu qua quy trình vận hành phù hợp, tắt
`Seed:Admin:Enabled` và xóa secret seed khỏi môi trường. Cơ chế này không phải
đường khôi phục quyền khi quản trị viên bị thu hồi role. Không cấu hình hai email
khác nhau trong Seed:Admin và Auth:BootstrapAdminEmail.

### Chạy migrator

Migration mới `DynamicRoleAuthorization` thêm metadata role và bảng
`authorization_audit_entries`, giữ nguyên dữ liệu roles/user_roles/role_permissions
và các migration đã triển khai. Không thu hẹp cột tên role Identity hiện có.
Tách list/read và seed admin không thay đổi schema; initializer bổ sung dữ liệu
permission và account dựa trên cấu hình, không sửa migration đã triển khai.
Trong production chạy deployment migrator trước API:

```powershell
dotnet run --project .\src\MatchingSports.Migrator --configuration Release
```

`GET /api/v1/auth/me` cần `profile.read`, `PUT /api/v1/auth/me` cần
`profile.update`, `GET /api/v1/sport` cần `sports.list`, GET sport/{sportId}
cần `sports.read`. Đăng ký, đăng nhập,
OTP và Google OAuth không cần permission trước đăng nhập.

### Kiểm thử PostgreSQL

Unit/architecture/policy tests không cần database. Test tích hợp RBAC dùng
database PostgreSQL rỗng, riêng biệt, tên bắt đầu `matchingsports_rbac_test_`.
Không trỏ biến này vào database development/production; fixture sẽ từ chối.
Không đặt connection string thật trong git:

```powershell
$env:MATCHINGSPORTS_RBAC_TEST_CONNECTION = "<isolated-test-connection>"
dotnet test .\MatchingSports.slnx --configuration Release --filter FullyQualifiedName~RoleAuthorizationDatabaseTests
```

Test kiểm tra migration + seed, role tùy chỉnh, nhiều membership, hợp quyền,
đăng ký email/Google theo role mặc định động, concurrency rollback cùng audit,
vô hiệu hóa role và seed chạy lại/song song không ghi đè cấu hình.
Fixture cũng chạy controller HTTP thật trên loopback với JWT thật để kiểm tra
401/403/400/409 và thu hồi permission với token chưa hết hạn.
Kiểm tra thêm tách list/read trên sport/role/permission, nâng cấp grant cũ chỉ một
lần, seed admin có mật khẩu hợp lệ, rollback khi mật khẩu yếu, và seed lại không
reset mật khẩu hay cấp lại role admin đã bị gỡ.
Khi không có biến môi trường, test tích hợp được skip rõ ràng.

## AutoMapper license

AutoMapper 16 được giữ theo yêu cầu. Development/testing có thể chạy không key,
nhưng production cần license hợp lệ. Cấu hình bằng `AUTOMAPPER_LICENSE_KEY`,
`LUCKYPENNY_LICENSE_KEY`, hoặc `AutoMapper__LicenseKey`.
