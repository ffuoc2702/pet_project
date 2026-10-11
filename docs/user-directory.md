# Danh sách người dùng và quản trị vai trò trên web

`GET /api/v1/users` yêu cầu `users.list` ở endpoint và MediatR pipeline.
HTTP request kế thừa `BaseRequest`, nhận `PageNumber`, `PageSize`, `SearchString`,
`FromDate`, `ToDate` và `Status` (`ACTIVE`, `SUSPENDED`, `DELETED`).
Search tìm email/tên hiển thị không phân biệt hoa thường; ngày lọc theo
`UpdatedAt`, gồm cả biên. Filter áp dụng trước count và phân trang, sắp xếp
email rồi ID. Query filter hiện có loại user đã soft-delete; lọc `DELETED`
không bỏ qua cơ chế này.

Response dùng `PaginatedResponse<UserListDto>` với tên, email, trạng thái,
xác thực email, lần đăng nhập gần nhất, audit metadata và role tóm tắt.
Không trả password hash, security stamp, token hoặc thông tin khóa tài khoản
Identity. Các role của mỗi user là dữ liệu chi tiết của bản ghi, không phải
API collection độc lập.

`GET /api/v1/roles` và `GET /api/v1/roles/matrix` nhận thêm `IsActive` nullable;
không truyền trả cả hai trạng thái. Ma trận phân trang theo hàng role, giữ đủ
catalog quyền gồm cả quyền chưa gán.

Web có `/users`, `/roles`, `/roles/matrix` trong nhóm sidebar Quản trị. Mỗi màn
dùng card, toolbar, bảng, loading và bộ lọc/phân trang URL dùng chung. Route
ma trận yêu cầu đủ `roles.list` và `permissions.list`; thiếu quyền không gọi API.
Loader chờ dữ liệu trước khi chuyển màn. UI có cả Việt/Anh và dùng theme chung.

Danh sách role hỗ trợ thêm/sửa theo `roles.create`/`roles.update`. Mô tả tối đa
200 ký tự. Vai trò mặc định khi đăng ký phải active; bỏ lựa chọn mặc định trong
form trước khi vô hiệu hóa. Danh sách người dùng hiện là màn đọc; không thêm
thao tác khóa, xóa hoặc gán role khi chưa có yêu cầu.

Ma trận lấy cột từ API, cho lọc nhóm theo prefix mã quyền, sửa khi có
`roles.permissions.update`. Lưu từng role qua endpoint hiện có và gửi toàn bộ
mã quyền, kể cả quyền ngoài nhóm đang hiển thị, cùng `ConcurrencyStamp` gốc.
Không tự retry/ghi đè 409. Nút tải lại hỏi bỏ thay đổi chưa lưu rồi đọc phiên bản
mới. Rời trang hoặc đổi filter/trang khi có draft cũng cảnh báo dữ liệu chưa lưu.
Sau khi lưu, làm mới session và cache role/user để quyền của actor cập nhật.
