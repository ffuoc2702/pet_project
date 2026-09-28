# Matching Sports MVP

Backend base cho nền tảng ghép đối và tìm người chơi đa môn, dựng từ bộ tài liệu Drive và data dictionary:

- PostgreSQL/PostGIS, ASP.NET Core 10, EF Core/Npgsql và .NET Aspire.
- 17 bảng, 187 cột, 27 khóa ngoại, các index/constraint/enum theo data dictionary.
- Seed idempotent cho `FOOTBALL`, `BADMINTON`, `PICKLEBALL` và 7 thể thức.
- API convention `/api/v1`, Problem Details và `X-Correlation-ID`.

Nguồn schema: [Database Data Dictionary - Sports Matching MVP](https://docs.google.com/spreadsheets/d/1h-v5nlTwegTSvhxeaL9iGHbKGi8vnQU8usB1V5ACfsY/edit).

## Clean Architecture

```text
src/MatchingSports.Domain
  Entities, enums, business vocabulary
        ^
src/MatchingSports.Application
  CQRS commands, queries, handlers, contracts and DTOs
        ^
src/MatchingSports.Infrastructure
  EF Core, PostgreSQL/PostGIS, migrations, query implementations
        ^
src/MatchingSports.Api
  HTTP endpoints, middleware, dependency composition

src/MatchingSports.AppHost
  Local orchestration: API, frontend, PostGIS, pgAdmin
```

Dependency direction luôn hướng vào trong:

- `Domain` không tham chiếu project nào.
- `Application` chỉ tham chiếu `Domain`.
- `Infrastructure` triển khai abstraction của `Application`.
- `Api` là composition root và không truy vấn EF Core trực tiếp.

## Feature-based structure

```text
src/
├── MatchingSports.Domain/
│   ├── Entities/
│   ├── ValueObjects/
│   ├── Enums/
│   ├── Events/
│   └── Exceptions/
├── MatchingSports.Application/
│   ├── Abstractions/
│   │   ├── Persistence/
│   │   └── Services/
│   ├── Features/
│   │   └── Catalog/
│   │       └── GetSportsCatalog/
│   └── Common/
├── MatchingSports.Infrastructure/
│   ├── Persistence/
│   │   ├── Configurations/
│   │   ├── Migrations/
│   │   └── Repositories/
│   └── Services/
└── MatchingSports.Api/
    ├── Controllers/
    ├── Contracts/
    ├── Middleware/
    ├── Filters/
    └── Program.cs
```

## CQRS convention

Mỗi use case được tổ chức theo feature và tách rõ read/write:

```text
src/MatchingSports.Application/Features/<Feature>/<QueryName>/
  <QueryName>Query.cs
  <QueryName>QueryHandler.cs
  <QueryName>Dto.cs

src/MatchingSports.Application/Features/<Feature>/<CommandName>/
  <CommandName>Command.cs
  <CommandName>CommandHandler.cs
  <CommandName>Validator.cs
  <CommandName>Result.cs

src/MatchingSports.Application/Abstractions/Persistence/
  I<Feature>Repository.cs
```

- Query/Command triển khai `IRequest<TResult>` của MediatR; handler triển khai `IRequestHandler<TRequest, TResult>`.
- Controller trong API inject `IMediator` và gửi request bằng `Send(...)`; không gọi trực tiếp handler hoặc repository.
- Handler inject `IMapper` để map request sang DTO truy vấn và map entity sang DTO trả về; lỗi được ghi bằng `ILogger<THandler>` rồi throw lại.
- Query không thay đổi trạng thái; Command đại diện thao tác ghi. Mỗi request và handler nằm trong file riêng theo feature.
- Handler chỉ điều phối use case qua abstraction của Application.
- Infrastructure triển khai read/write repository; truy vấn EF Core dùng LINQ, `AsNoTracking()` và async execution.
- Mỗi domain entity nằm trong một file riêng tại `src/MatchingSports.Domain/Entities`.
- Các FK được biểu diễn bằng cả trường `...Id` và navigation properties hai chiều; Fluent API cấu hình tường minh để có thể query qua quan hệ như `sport.Formats`, `listing.Requests` hoặc `match.Participants`.

`AddApplication(...)` tự đăng ký MediatR handler và AutoMapper profile. Với AutoMapper 16 trong môi trường production, truyền license key bằng configuration `AutoMapper:LicenseKey` (biến môi trường: `AutoMapper__LicenseKey`).

## Chạy local

Yêu cầu Docker và .NET 10 SDK:

```powershell
dotnet restore .\MatchingSports.slnx
dotnet run --project .\src\MatchingSports.AppHost
```

AppHost sử dụng image `postgis/postgis:18-3.6`. Ở Development, API tự apply migration và seed catalog khi khởi động.

## Migration

Migration đầu tiên và SQL tương ứng nằm tại `src/MatchingSports.Infrastructure/Persistence/Migrations`.

Tạo migration tiếp theo:

```powershell
dotnet ef migrations add <Name> `
  --project .\src\MatchingSports.Infrastructure `
  --startup-project .\src\MatchingSports.Infrastructure `
  --output-dir Persistence\Migrations
```

## Endpoint nền

- `GET /api/v1/` — thông tin service.
- `GET /api/v1/catalog/sports` — danh mục môn và thể thức đã seed.
- `GET /health`, `GET /alive` — health/liveness ở Development.
- `GET /openapi/v1.json` — OpenAPI ở Development.
