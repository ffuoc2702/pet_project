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

## AutoMapper license

AutoMapper 16 được giữ theo yêu cầu. Development/testing có thể chạy không key,
nhưng production cần license hợp lệ. Cấu hình bằng `AUTOMAPPER_LICENSE_KEY`,
`LUCKYPENNY_LICENSE_KEY`, hoặc `AutoMapper__LicenseKey`.
