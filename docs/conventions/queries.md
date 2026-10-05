# Convention query

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

## Tham chiếu

- [Phân trang](../pagination.md)
- [Danh tính và phân quyền](../authorization.md)
- [Persistence](../persistence.md)
- [Kiểm thử](../testing.md)

Query chỉ đọc dùng projection hoặc `AsNoTracking()` trong Infrastructure; không commit.
