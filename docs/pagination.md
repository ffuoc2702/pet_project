# Phân trang API list

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

Xem [convention query](conventions/queries.md) cho BaseQuery, mapping và filter.
