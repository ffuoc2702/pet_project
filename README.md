# MatchingSports

Nền tảng tìm người chơi và đối thủ thể thao theo môn, thể thức, vị trí và thời gian. Repository gồm backend ASP.NET Core, ứng dụng web React và công cụ mô phỏng dữ liệu demo.

Backend đã triển khai hồ sơ người chơi, quản lý đội, tìm kèo, đăng ký tham gia, quản lý trận, theo dõi kèo và thông báo. Web hiện có nền tảng giao diện, xác thực và các màn quản trị môn thể thao, người dùng, vai trò/quyền; những luồng nghiệp vụ còn lại được sử dụng qua API hoặc simulator.

## Chức năng đã triển khai

### Backend

| Nhóm | Phạm vi hiện có |
| --- | --- |
| Danh tính | Đăng ký email, xác minh/gửi lại OTP, đăng nhập, refresh/logout, quên/đặt lại mật khẩu, đăng nhập Google. Contract token cho client và contract cookie riêng cho web. |
| Hồ sơ | Đọc/sửa hồ sơ cá nhân; thêm/sửa/xóa hồ sơ môn chơi, trình độ, giới thiệu và thể thức ưa thích; đọc hồ sơ công khai người chơi/đội theo quyền. |
| Môn thể thao | Danh sách có tìm kiếm, lọc, phân trang; chi tiết, tạo/sửa môn và thể thức; kích hoạt/vô hiệu hóa, giữ dữ liệu tham chiếu. |
| Phân quyền | RBAC nhiều role, catalog permission, ma trận quyền, tạo/sửa role, thay thế tập quyền và membership; role mặc định cho đăng ký mới. |
| Người dùng | Danh sách người dùng phục vụ quản trị, tìm kiếm/lọc và quản lý role theo permission. |
| Đội nhóm | Tạo/sửa/xóa mềm đội, thành viên, lời mời, yêu cầu gia nhập, rời/loại thành viên, bổ nhiệm/thu hồi đội phó, chuyển đội trưởng; kiểm tra quota và quyền trên đội. |
| Kèo và yêu cầu | Tìm người vãng lai (`FIND_PLAYERS`) hoặc đối thủ cho đội (`FIND_OPPONENT`); tìm theo vị trí/bán kính PostGIS, thời gian, môn/thể thức và trình độ; tạo/sửa/hủy kèo, gửi/duyệt/từ chối/rút yêu cầu. |
| Trận đấu | Tạo trận khi duyệt yêu cầu đầu tiên; xác nhận khi đủ chỗ, hủy, hoàn tất, đọc liên hệ theo quyền; hủy quản trị có audit; tự hết hạn và nhắc lịch trận. |
| Theo dõi kèo | Lưu tiêu chí tìm vãng lai, sửa/tạm dừng/kích hoạt/hủy; worker tìm kèo phù hợp, chống thông báo trùng theo người dùng × kèo. |
| Thông báo | Gửi một lần hoặc cron theo múi giờ, chọn user/team/toàn bộ người dùng, preview, kích hoạt/hủy; history snapshot, inbox cá nhân, số chưa đọc và đánh dấu đã đọc. In-app và email SMTP, outbox/retry/cleanup. |
| Cấu hình hệ thống | Giới hạn đội/thành viên/đội phó/yêu cầu, thời hạn lời mời, retry và retention, quota/bán kính/cửa sổ theo dõi kèo, thời gian nhắc trận. |
| Nền tảng | Audit timestamp/actor, optimistic concurrency, transaction, Problem Details, correlation ID, rate limiting, health checks, Swagger, log Serilog ECS và OpenTelemetry. |

### Web

| Màn/luồng | Đường dẫn |
| --- | --- |
| Trang chủ | `/` |
| Xác thực và khôi phục tài khoản | `/login`, `/register`, `/verify-email`, `/forgot-password`, `/reset-password`, `/auth/callback` |
| Danh sách, tạo, chi tiết, sửa và đổi trạng thái môn thể thao | `/sports`, `/sports/new`, `/sports/:sportId`, `/sports/:sportId/edit` |
| Danh sách người dùng và quản lý role được gán | `/users` |
| Quản lý vai trò và ma trận quyền | `/roles`, `/roles/matrix` |
| Ngôn ngữ và giao diện | `/preferences` |
| Bản đồ/tọa độ | `/location` |
| Bộ component UI, chỉ trong development | `/dev/ui` |

Web hỗ trợ tiếng Việt/Anh, light/dark/system, preset màu, responsive sidebar, breadcrumb, filter/phân trang trên URL, tải dữ liệu trước khi chuyển trang và cảnh báo form chưa lưu. Tùy chọn tài khoản đồng bộ qua API; tùy chọn khách lưu local. Google Maps là tích hợp tùy chọn, vẫn nhập tọa độ thủ công được khi chưa cấu hình key.

### Phạm vi chưa hoàn thành

- Chưa có màn web cho hồ sơ cá nhân/công khai, đội nhóm, kèo/yêu cầu/trận, theo dõi kèo, hộp thông báo, cấu hình thông báo và cấu hình hệ thống.
- Chưa có ứng dụng mobile, đăng ký thiết bị/token, adapter push hoặc SMS. Push/SMS chưa được kích hoạt để gửi thực tế.
- Chưa có rating/thành tích hoặc list/search hồ sơ công khai. Hồ sơ “công khai” hiện yêu cầu đăng nhập và permission.
- Catalog permission và danh sách màn thiết kế có thể bao gồm phần dự kiến; không đồng nghĩa với API hoặc giao diện đã triển khai.

## Công nghệ và cấu trúc

| Thành phần | Công nghệ |
| --- | --- |
| Backend | .NET 10, ASP.NET Core, Identity, MediatR, AutoMapper |
| Persistence | EF Core, Npgsql, PostgreSQL/PostGIS, NetTopologySuite |
| Web | React 19, TypeScript strict, Vite 8, React Router, TanStack Query |
| UI | Ant Design 6, Bootstrap cho grid/container, i18next |
| Vận hành local | .NET Aspire, Docker, pgAdmin, migrator riêng |
| Kiểm thử | Backend test project, Vitest/Testing Library, Playwright, PostgreSQL integration tests |

```text
backend/
  src/
    MatchingSports.Domain/          Entity, value object, invariant, hành vi nghiệp vụ
    MatchingSports.Application/     CQRS theo feature, DTO, abstraction, pipeline
    MatchingSports.Infrastructure/  EF Core, repository, migration, Identity, adapter/worker
    MatchingSports.Api/             HTTP contract, controller, auth, composition root
    MatchingSports.Shared/          Constants, mã lỗi và tiện ích thuần
    MatchingSports.Migrator/        Apply migration và seed
    MatchingSports.AppHost/         Điều phối database → migrator → API → web
  tests/MatchingSports.Tests/
  tools/MatchingSports.DemoSimulator/
  scripts/                         Bash: container, integration test, demo
frontend/
  src/app/                         Router, layout, providers, navigation
  src/features/                    Auth, sports, users, roles, preferences, location
  src/shared/                      API client, UI, i18n, theme, hooks, integrations
docs/                              Kiến trúc, API, nghiệp vụ và vận hành
```

Dependency giữa các project:

```text
Domain         → Shared
Application    → Domain, Shared
Infrastructure → Application, Domain, Shared
Api            → Application, Infrastructure, Shared
Shared         → không tham chiếu project MatchingSports khác
```

Domain giữ invariant và chuyển trạng thái, không gọi I/O. Application điều phối use case và commit qua `IUnitOfWork`; Infrastructure triển khai persistence và dịch vụ ngoài. Controller gửi request qua MediatR, không truy vấn repository/DbContext trực tiếp. Query chỉ đọc không commit; repository không tự `SaveChanges`.

Quy tắc đóng góp nằm trong [AGENTS.md](AGENTS.md), [SKILL.md](SKILL.md) và [kiến trúc](docs/architecture.md). Model `User`/`Role` hiện giữ base Identity; dependency Identity này không phải mẫu cho entity nghiệp vụ mới.

## Chạy local

Các lệnh dưới đây chạy từ **thư mục gốc repository**, trừ khi có `cd`. Dùng Bash; trên Windows dùng Git Bash.

Yêu cầu:

- .NET SDK `10.0.401` hoặc bản patch tương thích theo [global.json](backend/global.json).
- Node.js 22.12+ và npm; engine được khai báo tại [package.json](frontend/package.json).
- Docker Desktop/Linux containers để chạy PostgreSQL/PostGIS hoặc Aspire.
- Python 3 và curl khi dùng script container/test/demo.

### Cách 1: .NET Aspire

```bash
dotnet restore ./backend/MatchingSports.slnx --locked-mode
cd frontend
npm ci
cd ..
dotnet run --project ./backend/src/MatchingSports.AppHost
```

AppHost tạo PostgreSQL với image `postgis/postgis:18-3.6`, volume dữ liệu và pgAdmin; đợi migrator hoàn tất rồi chạy API và Vite. Mở URL resource trên Aspire dashboard. Nếu có proxy local ghi đè trong `.env.local`, kiểm tra lại để frontend trỏ đúng API của Aspire.

### Cách 2: API container + Vite local

Nếu chưa có PostgreSQL local, tạo container:

```bash
docker run --name matchingsports-postgres-dev \
  -e POSTGRES_DB=matchingsports \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  -v matchingsports-postgres-dev-data:/var/lib/postgresql \
  -d postgis/postgis:18-3.6

bash ./backend/scripts/start-container.sh
```

Đây là thông tin đăng nhập cho database development local. Script cần container PostgreSQL đang chạy, build image, chạy migrator rồi kiểm tra `/health`. Cấu hình runtime được giữ trong `backend/.artifacts/container/`, không đưa vào Git. Compose không tự tạo PostgreSQL.

Chạy frontend ở terminal khác:

```bash
cd frontend
npm ci
cp .env.example .env.local
npm run dev
```

Nếu đã có `.env.local`, cập nhật file hiện có trước khi chạy, tránh ghi đè cấu hình riêng. Mẫu mặc định dùng `SERVER_HTTP=http://127.0.0.1:8080`; Vite proxy `/api` và `/signin-google` sang backend. Bỏ `SERVER_HTTPS` cũ nếu muốn dùng proxy HTTP.

| Dịch vụ | URL mặc định với cách chạy container |
| --- | --- |
| Web | `http://127.0.0.1:5173` (xem terminal nếu cổng thay đổi) |
| API | `http://127.0.0.1:8080/api/v1` |
| Swagger | `http://127.0.0.1:8080/swagger` |
| OpenAPI | `http://127.0.0.1:8080/swagger/v1/swagger.json` |
| Health/liveness | `http://127.0.0.1:8080/health`, `http://127.0.0.1:8080/alive` |

Chi tiết đổi container/database/port và quản lý stack: [container local](docs/local-containers.md).

Cũng có thể chạy API trực tiếp với PostgreSQL local:

```bash
dotnet run --project ./backend/src/MatchingSports.Api --launch-profile https
```

Profile này dùng `https://localhost:7406` và `http://localhost:5381`; cấu hình Vite `SERVER_HTTPS=https://localhost:7406`. API Development bật `Database:Initialize` trong appsettings để tự migrate/seed; AppHost và Compose tắt cờ này vì đã chạy migrator riêng.

## Cấu hình và dữ liệu khởi tạo

Seed idempotent gồm `FOOTBALL`, `BADMINTON`, `PICKLEBALL` và 7 thể thức; role/permission nền và cấu hình hệ thống. Seed không tự khôi phục permission đã bị quản trị viên thu hồi. Role mặc định cho đăng ký mới được quản lý bằng `IsDefaultForRegistration`, không suy luận từ tên role.

Tài khoản quản trị ban đầu **không bật mặc định**. Để khởi tạo, cấu hình `Seed:Admin:Enabled`, `Email`, `Password`, `DisplayName` cho host thực hiện seed. Xem [phân quyền](docs/authorization.md).

Secret server dùng environment hoặc user-secrets. Ví dụ cho API chạy trực tiếp:

```bash
dotnet user-secrets set "Auth:Jwt:SigningKey" "<random-secret-at-least-32-bytes>" --project ./backend/src/MatchingSports.Api
dotnet user-secrets set "Auth:Otp:Pepper" "<independent-random-secret-at-least-32-bytes>" --project ./backend/src/MatchingSports.Api
```

| Nhóm cấu hình | Mục đích |
| --- | --- |
| `ConnectionStrings:matchingsports` | Kết nối PostgreSQL/PostGIS. |
| `Auth:Jwt`, `Auth:Otp` | JWT, refresh session và OTP; signing key/pepper tối thiểu 32 byte. Thay các giá trị Development mẫu khi cấu hình môi trường thật. |
| `Auth:Gmail` | SMTP: host/port/SSL, sender, username, app password; cần cho OTP và email notification. |
| `Auth:Google` | OAuth client ID/secret, callback path `/signin-google`. |
| `DataProtection:KeyDirectory` | Key ring bền vững, dùng chung giữa replica khi triển khai. |
| `Notifications:WorkersEnabled` | Bật/tắt notification workers; mặc định bật. |
| `Matching:WorkersEnabled` | Bật/tắt matching worker; mặc định bật. |
| `Matching:Notifications:EmailEnabled` | Email sự kiện matching; mặc định tắt. Watch có lựa chọn email riêng, vẫn cần SMTP hợp lệ. |
| `AutoMapper:LicenseKey` | License key khi môi trường sử dụng AutoMapper yêu cầu. |

Environment dùng dấu `__` thay `:`, ví dụ `ConnectionStrings__matchingsports`. Migrator là host riêng: cấu hình database/seed cho migrator khi nó thực hiện khởi tạo.

Frontend đọc [.env.example](frontend/.env.example). `VITE_*` là cấu hình public lúc build; Google Maps dùng `VITE_GOOGLE_MAPS_API_KEY` và `VITE_GOOGLE_MAPS_MAP_ID`. Không đặt secret server trong `VITE_*`. Để thử Google OAuth local, bật `DEV_HTTPS=true`, tin certificate local và đăng ký đúng redirect URI `https://<web-host>:<port>/signin-google`.

## API, quyền và concurrency

- Prefix API: `/api/v1`. Route hiện tại dùng `/sports`, `/roles`, `/permissions`; danh sách đầy đủ nằm trong controller và Swagger Development.
- Success dùng `BaseResponse<T>`/`PaginatedResponse<T>`; lỗi dùng Problem Details kèm `code`, `traceId`, `correlationId`. Hỗ trợ header `X-Correlation-ID`.
- List độc lập có phân trang; filter/search áp dụng trước count và paging. Xem [pagination](docs/pagination.md).
- Quyền là hợp permission từ các role đang hoạt động, không có ADMIN bypass. `*.list` và `*.read` độc lập; quyền hệ thống vẫn phải đi cùng ownership/vai trò trên tài nguyên.
- Actor lấy từ danh tính đã xác thực; audit ghi ở persistence. Audit context không thay thế authorization.
- Resource dùng GUID concurrency trả `concurrencyToken`; Identity giữ `concurrencyStamp`. Lệnh yêu cầu phiên bản phải gửi giá trị mới nhất; conflict trả HTTP 409, cần tải lại và quyết định gửi lại.
- Web giữ access token trong memory, refresh token trong cookie HttpOnly và dùng CSRF cho auth ghi. Contract `/auth` dạng token được giữ cho client/mobile; `/auth/web` phục vụ web.

Tham khảo [Postman collection](docs/api/MatchingSports.postman_collection.json) và Swagger của backend đang chạy; khi contract khác nhau, đối chiếu controller/HTTP contract hiện tại.

## Migration và triển khai

API production mặc định không tự migrate. Chạy `MatchingSports.Migrator` như deployment job trước khi khởi động API:

```bash
# Cấp ConnectionStrings__matchingsports qua secret của môi trường triển khai.
dotnet run --project ./backend/src/MatchingSports.Migrator --configuration Release
```

Migrator apply migration và seed dữ liệu nền. Quy trình tạo migration, persistence và concurrency: [docs/persistence.md](docs/persistence.md).

Web production và API dùng cùng origin, HTTPS và reverse proxy; SPA fallback cho route web. AppHost đã cấu hình publish frontend vào `wwwroot` của API. Khi triển khai riêng, build `frontend/dist` và proxy `/api`, `/signin-google` tới backend. Giữ DataProtection key ring bền vững và cấu hình secret theo host. Chi tiết: [frontend README](frontend/README.md), [web foundation](docs/web-foundation.md).

SMTP có semantics at-least-once: provider đã nhận nhưng commit database thất bại có thể dẫn tới gửi lặp khi retry. Retry không tăng lượt phát notification. Chi tiết lease, transaction, retention và giới hạn kênh tại [notifications](docs/notifications.md).

## Demo và kiểm thử

Simulator chạy use case qua MediatR, mặc định tạo 50 người dùng, 6 đội và 15 kèo bóng đá 5v5; mô phỏng lời mời, đăng ký/duyệt, xác nhận/hủy, nhắc lịch, watch và đọc inbox. Đây là công cụ demo nghiệp vụ/API, không phải benchmark HTTP.

```bash
cd backend
bash ./scripts/demo.sh --run-id cv-demo-01
```

Script hỏi mật khẩu chung qua secure prompt; báo cáo/checkpoint nằm trong `backend/.artifacts/demo-simulator/`. Có chế độ database test riêng, resume và tùy chỉnh quy mô. Matching/reminder jobs xử lý toàn database; xem [demo simulator](docs/demo-simulator.md) trước khi chạy trên dữ liệu dùng chung.

Kiểm tra backend, chạy từ `backend/`:

```bash
dotnet restore ./MatchingSports.slnx --locked-mode
dotnet build ./MatchingSports.slnx --configuration Release --no-restore
dotnet test ./MatchingSports.slnx --configuration Release --no-build --no-restore
dotnet format ./MatchingSports.slnx --verify-no-changes --no-restore
```

Để chạy cả PostgreSQL/PostGIS integration tests với container local:

```bash
bash ./scripts/test.sh --no-build
```

Nếu không có `MATCHINGSPORTS_RBAC_TEST_CONNECTION`, `dotnet test` trực tiếp bỏ qua integration tests cần PostgreSQL. Script lấy kết nối từ container; mỗi test tạo/xóa database riêng có prefix `matchingsports_rbac_test_`. Xem [testing](docs/testing.md).

Kiểm tra frontend, chạy từ `frontend/`:

```bash
npm run typecheck
npm run lint
npm run format:check
npm test
npm run build
npm run browser:install
npm run test:e2e
```

CI tại [.github/workflows/ci.yml](.github/workflows/ci.yml) cấu hình các kiểm tra backend/frontend và PostgreSQL integration tests. E2E web dùng mock API; không thay thế kiểm thử tích hợp backend.

## Tài liệu theo phạm vi

| Chủ đề | Tài liệu |
| --- | --- |
| Kiến trúc và convention | [Architecture](docs/architecture.md), [commands](docs/conventions/commands.md), [queries](docs/conventions/queries.md) |
| Persistence và audit | [Persistence](docs/persistence.md), [auditing](docs/auditing.md) |
| Phân quyền và người dùng | [Authorization](docs/authorization.md), [user directory](docs/user-directory.md) |
| Hồ sơ | [Hồ sơ thể thao](docs/user-sport-profiles.md), [hồ sơ công khai](docs/public-profiles.md) |
| Đội và kèo/trận | [Teams](docs/teams.md), [matching](docs/matching.md) |
| Thông báo và cấu hình | [Notifications](docs/notifications.md), [system settings](docs/system-settings.md) |
| Web/UI | [Frontend](frontend/README.md), [UI base](frontend/docs/ui-base.md), [web foundation](docs/web-foundation.md) |
| Vận hành và kiểm thử | [Container local](docs/local-containers.md), [testing](docs/testing.md), [demo](docs/demo-simulator.md) |
| Thiết kế màn hình | [Design inventory](docs/design/README.md) — gồm cả màn dự kiến, không phải danh sách màn web đã hoàn thành |
