# Kiểm thử

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

## Query và phân trang

Khi thêm/sửa API list, kiểm thử trang mặc định, trang kế tiếp, trang rỗng,
metadata, validation qua MediatR, quyền và cancellation phù hợp mức rủi ro.
Khi thêm query list, kiểm tra cấu hình AutoMapper, field kế thừa/default,
filter riêng, actor tin cậy, filter thực tế và metadata sau lọc.
Dùng `PaginationTests` làm tham chiếu và đọc [phân trang](pagination.md),
[convention query](conventions/queries.md) cho yêu cầu chi tiết.

Các lệnh restore/build/test/format và quy tắc package/lock file nằm trong
[AGENTS.md](../AGENTS.md#lệnh-kiểm-tra).
