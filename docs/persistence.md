# Persistence và bảo mật

Entity base, nguồn actor và quy tắc stamp audit được mô tả trong
[audit](auditing.md). Metadata audit do Infrastructure ghi, không nhận từ body.
Transaction, lease, SQL lock và migration thông báo được mô tả trong
[notifications](notifications.md).

- EF configuration và migration nằm ở Infrastructure. Không sửa migration đã
  triển khai; tạo migration mới khi schema thay đổi.
- Giữ convention concurrency token hiện có khi cập nhật entity có cạnh tranh ghi.
  Không tự ghi đè dữ liệu khi gặp conflict.
- Secret nằm trong user-secrets/environment; không commit token/password/OTP.
  Không log password, OTP, access token, refresh token hoặc dữ liệu riêng tư đầy đủ.

## Repository và transaction

`GetForUpdateAsync` trả entity tracked; đây không phải lời hứa khóa hàng database.
Repository không tự gọi SaveChanges; handler điều phối commit qua `IUnitOfWork`.
Query chỉ đọc dùng projection hoặc `AsNoTracking()` trong Infrastructure.
Truyền CancellationToken xuyên suốt các lời gọi async hỗ trợ cancellation.

Với repository list, đọc [phân trang](pagination.md) và
[convention query](conventions/queries.md) trước khi sửa filter, count hoặc paging.
Với schema, concurrency và API/persistence, đọc [kiểm thử](testing.md).
