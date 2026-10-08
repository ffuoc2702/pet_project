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

## PostgreSQL integration tests

Sau khi restore, chạy từ backend/:

```powershell
.\scripts\Test.ps1
```

Script mặc định dùng container đang chạy `matchingsports-postgres-dev`, lấy
thông tin đăng nhập và cổng đã publish qua Docker, rồi đặt
`MATCHINGSPORTS_RBAC_TEST_CONNECTION` chỉ trong quá trình chạy test. Không ghi
mật khẩu vào repo hoặc in chuỗi kết nối. Dùng `-DatabaseContainer <tên>` để
chọn container khác, hoặc `-NoBuild` khi đã build Release.

Nếu dùng PostgreSQL ngoài Docker, đặt biến môi trường trên trước khi chạy
script. Database trong chuỗi kết nối phải bắt đầu bằng
`matchingsports_rbac_test_`; không cần tạo database đó trước. Tài khoản cần
quyền tạo/xóa database và cài extension PostGIS. Mỗi integration test tạo
database với tên ngẫu nhiên riêng và dọn trong finally; không chạy migration
hoặc seed vào database ứng dụng. Chạy `dotnet test` trực tiếp mà không có
biến môi trường vẫn bỏ qua các integration test PostgreSQL.
