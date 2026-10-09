# Danh sách màn MatchingSports

Trạng thái: đã tạo [file Figma mới](https://www.figma.com/design/Hy26nYnp9g5oj156rML1F7), hoàn thành 13/84 màn với 26 frame desktop/mobile. Figma MCP Starter đã chặn do giới hạn lượt gọi. [Plugin tiếp tục](figma/README.md) đã được chuẩn bị cho 71 màn còn lại, chưa chạy trong Figma.

React + Bootstrap cho grid/container; Ant Design cho Form, Table, Modal, Drawer, Select, DatePicker, Tabs, Pagination, Alert, Result và notification. Desktop 1440 px; mobile 390 px. Hệ chữ theo system font hiện có; xác minh font khả dụng trước khi dựng Figma.

## Danh tính

Vai trò: Mọi người.

| ID | Màn / trạng thái | Dạng |
|---|---|---|
| AUTH-01 | Đăng nhập | page |
| AUTH-02 | Đăng ký | page |
| AUTH-03 | Xác minh email và gửi lại OTP | page |
| AUTH-04 | Quên mật khẩu | page |
| AUTH-05 | Đặt lại mật khẩu | page |
| AUTH-06 | Kết quả đăng nhập Google | state |
| AUTH-07 | Phiên đăng nhập hết hạn | state |
| AUTH-08 | Xác nhận đăng xuất | modal |

## Hồ sơ

Vai trò: Người chơi.

| ID | Màn / trạng thái | Dạng |
|---|---|---|
| PROFILE-01 | Hồ sơ của tôi | page |
| PROFILE-02 | Sửa hồ sơ của tôi | page |
| PROFILE-03 | Thêm hoặc sửa hồ sơ môn chơi | drawer |
| PROFILE-04 | Xác nhận xoá hồ sơ môn chơi | modal |
| PROFILE-05 | Hồ sơ công khai của người chơi khác | page |

## Đội

Vai trò: Người chơi / thành viên / đội phó / đội trưởng.

| ID | Màn / trạng thái | Dạng |
|---|---|---|
| TEAM-01 | Tìm đội và đội của tôi | page |
| TEAM-02 | Hồ sơ công khai của đội | page |
| TEAM-03 | Chi tiết đội của tôi | page |
| TEAM-04 | Thành viên và quyền quản lý theo vai trò | page |
| TEAM-05 | Tạo đội | page |
| TEAM-06 | Sửa thông tin đội | drawer |
| TEAM-07 | Mời thành viên bằng UserId và xem trước hồ sơ | modal |
| TEAM-08 | Lời mời đến đội của tôi | page |
| TEAM-09 | Yêu cầu vào đội cần xử lý | page |
| TEAM-10 | Lời mời tôi nhận được | page |
| TEAM-11 | Yêu cầu vào đội của tôi | page |
| TEAM-12 | Gửi yêu cầu vào đội | modal |
| TEAM-13 | Duyệt hoặc từ chối yêu cầu vào đội | modal |
| TEAM-14 | Bổ nhiệm hoặc thu hồi đội phó | modal |
| TEAM-15 | Chuyển đội trưởng | modal |
| TEAM-16 | Loại thành viên | modal |
| TEAM-17 | Rời đội | modal |
| TEAM-18 | Xoá đội đủ điều kiện | modal |
| TEAM-19 | Xoá đội bị chặn do còn thành viên hoặc trận | state |

## Tìm và quản lý kèo

Vai trò: Người chơi / quản lý đội.

| ID | Màn / trạng thái | Dạng |
|---|---|---|
| LISTING-01 | Tìm kèo theo môn, bán kính và thời gian | page |
| LISTING-02 | Không có kèo và tạo theo dõi | state |
| LISTING-03 | Chi tiết kèo tuyển người vãng lai | page |
| LISTING-04 | Chi tiết kèo tìm đội đối thủ | page |
| LISTING-05 | Đăng ký tham gia cá nhân hoặc đại diện đội | modal |
| LISTING-06 | Yêu cầu tham gia của tôi | page |
| LISTING-07 | Rút yêu cầu tham gia | modal |
| LISTING-08 | Kèo do tôi hoặc đội của tôi tổ chức | page |
| LISTING-09 | Tạo kèo tuyển người hoặc tìm đối thủ | page |
| LISTING-10 | Sửa kèo chưa có người được duyệt | page |
| LISTING-11 | Danh sách yêu cầu và chi tiết ứng viên | page |
| LISTING-12 | Duyệt chỗ cuối hoặc từ chối ứng viên | modal |
| LISTING-13 | Huỷ kèo chưa có trận | modal |
| LISTING-14 | Kèo hết hạn hoặc đã đóng | state |

## Theo dõi kèo

Vai trò: Người chơi.

| ID | Màn / trạng thái | Dạng |
|---|---|---|
| WATCH-01 | Danh sách theo dõi của tôi | page |
| WATCH-02 | Tạo hoặc sửa theo dõi | drawer |
| WATCH-03 | Chi tiết theo dõi với trạng thái | page |
| WATCH-04 | Tạm dừng, kích hoạt lại hoặc huỷ theo dõi | modal |

## Trận đấu

Vai trò: Người tham gia / chủ kèo / quản lý đội.

| ID | Màn / trạng thái | Dạng |
|---|---|---|
| MATCH-01 | Danh sách trận cá nhân và trận của đội | page |
| MATCH-02 | Chi tiết trận chưa xác nhận | page |
| MATCH-03 | Chi tiết trận xác nhận và thông tin liên hệ | page |
| MATCH-04 | Xác nhận trận đủ chỗ | modal |
| MATCH-05 | Huỷ trận với lý do | modal |
| MATCH-06 | Hoàn tất trận sau giờ kết thúc | modal |
| MATCH-07 | Trận đã huỷ hoặc hoàn tất | state |

## Thông báo

Vai trò: Người chơi.

| ID | Màn / trạng thái | Dạng |
|---|---|---|
| INBOX-01 | Hộp thông báo và số chưa đọc | page |
| INBOX-02 | Chi tiết thông báo và đánh dấu đã đọc | drawer |
| INBOX-03 | Thông báo kèo phù hợp hoặc nhắc lịch trận | state |
| INBOX-04 | Tài nguyên không còn tồn tại khi mở thông báo | state |

## Quản trị

Vai trò: Người có permission quản trị tương ứng.

| ID | Màn / trạng thái | Dạng |
|---|---|---|
| ADMIN-01 | Danh sách môn thể thao | page |
| ADMIN-02 | Tạo hoặc sửa môn và thể thức chơi | page |
| ADMIN-03 | Ngừng hoạt động môn hoặc thể thức | modal |
| ADMIN-04 | Danh sách vai trò | page |
| ADMIN-05 | Tạo hoặc sửa vai trò | drawer |
| ADMIN-06 | Ma trận vai trò và permission | page |
| ADMIN-07 | Danh sách và chi tiết permission | page |
| ADMIN-08 | Tra cứu và thay vai trò người dùng bằng UserId | page |
| ADMIN-09 | Danh sách cấu hình thông báo | page |
| ADMIN-10 | Soạn nội dung, đối tượng và lịch gửi | page |
| ADMIN-11 | Xem trước người nhận và các mốc lịch | page |
| ADMIN-12 | Chi tiết cấu hình và lịch sử gửi | page |
| ADMIN-13 | Kích hoạt hoặc huỷ lịch gửi | modal |
| ADMIN-14 | Cấu hình hệ thống, giới hạn và thời gian nhắc trận | page |
| ADMIN-15 | Huỷ trận bằng quyền quản trị | modal |

## Trạng thái dùng chung

Vai trò: Mọi người.

| ID | Màn / trạng thái | Dạng |
|---|---|---|
| COMMON-01 | Đang tải và skeleton | state |
| COMMON-02 | Danh sách trống | state |
| COMMON-03 | Lỗi validation theo field | state |
| COMMON-04 | Không có quyền 403 | state |
| COMMON-05 | Không tìm thấy 404 | state |
| COMMON-06 | Dữ liệu đã thay đổi 409 và tải lại | modal |
| COMMON-07 | Mất kết nối hoặc dịch vụ lỗi | state |
| COMMON-08 | Vị trí bị từ chối và nhập tọa độ thủ công | state |

## Điều kiện nghiệp vụ

- Public profiles require authentication; no email, contact note, internal members or rating.
- Team captain alone transfers captaincy, assigns deputies and deletes team. Deputy manages invitations/requests and ordinary members.
- Deleting team requires only captain active and no unfinished listing/match.
- Contact opens only to active participants in CONFIRMED match.
- Confirmed match reminder lead is global admin system setting; not a personal preference.
- Watch is personal FIND_PLAYERS; radius and time only, no level filter or opponent/team watch.
- 409 requires reload and a deliberate new submission; never silently overwrite.
- Admin user-role assignment uses known UserId; no user directory/search endpoint exists.
- Notification ReadAt is independent of email delivery status. In-app/email only.
- Search geolocation fields latitude/longitude/radius must go together; manual coordinates support denied location.

Mapping 96 endpoint sang màn, body fields và query fields nằm trong [screen-inventory.json](screen-inventory.json). Health/API info không cần một màn nghiệp vụ riêng. Các màn tổng hợp dùng lại endpoint hiện có, không giả định endpoint dashboard hay tìm kiếm người dùng mới. Mobile áp dụng navigation gọn, card thay bảng rộng, filter drawer và action cố định ở cuối màn.

## Kiểm chứng sau khi dựng Figma

Mỗi màn phải có layer chữ và component chỉnh sửa được; kiểm tra tràn/cắt chữ, thứ tự đọc, loading/empty/error, quyền theo vai trò, trạng thái yêu cầu/trận và xác nhận action. Liên kết prototype cho các luồng tìm kèo → đăng ký → duyệt → xác nhận; không có kèo → theo dõi → thông báo; lời mời/yêu cầu đội; chuyển đội trưởng; gửi thông báo quản trị. Node IDs được cập nhật khi canvas thực sự được tạo.
