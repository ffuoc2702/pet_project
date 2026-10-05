# Kiến trúc MatchingSports

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
