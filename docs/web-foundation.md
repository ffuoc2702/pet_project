# Web foundation và contract dùng chung mobile

Frontend nằm tại frontend/, convention tại frontend/AGENTS.md.
Xem frontend/README.md để cấu hình local, Google Maps, navigation, theme và deploy.

## Auth web

Các endpoint mới /api/v1/auth/web dùng lại LoginCommand, RefreshTokenCommand,
LogoutCommand và ExternalLoginCommand. Không thay đổi contract auth token cũ.

- GET /csrf trả requestToken, thiết lập antiforgery cookie.
- POST /login nhận email/password, trả accessToken/tokenType/accessTokenExpiresAt/user.
- POST /refresh đọc refresh cookie; không lấy token từ HTTP body.
- POST /logout thu hồi session và xóa refresh cookie.
- GET /google khởi động external OAuth; callback thiết lập cookie và redirect
  cố định /auth/callback trên cùng origin. Token không xuất hiện trong URL.

Các POST trên yêu cầu X-CSRF-TOKEN; lỗi trả INVALID_CSRF_TOKEN qua Problem Details.
Refresh cookie production __Host-matchingsports.refresh dùng HttpOnly, Secure,
SameSite=Lax, Path=/, không có Domain. Phản hồi auth không cache.
Development có tên cookie riêng và chỉ yêu cầu Secure khi request HTTPS.
Production dùng web/API cùng origin; không bật CORS credentials đại trà.
DataProtection__KeyDirectory cấu hình key ring bền vững và dùng chung giữa replica;
application name cố định MatchingSports. Volume phải được bảo vệ ở hạ tầng.

## Preferences cá nhân

GET /api/v1/me/preferences trả BaseResponse<UserPreferencesDto?>.
Chưa lưu tùy chọn trả data=null, không tạo bản ghi trong query.
PUT nhận languageCode, colorMode, themePreset, concurrencyToken.
Lần tạo đầu tiên gửi concurrencyToken=null; cập nhật gửi token hiện tại.
Actor lấy qua ICurrentUser, không nhận UserId/audit từ body.
Entity có audit và GUID concurrency; response map sau commit.
Thiếu/stale token khi bản ghi đã có trả CONCURRENCY_CONFLICT; không ghi đè.

LanguageCode: vi/en; ColorMode: light/dark/system; ThemePreset: blue/green/purple.
Đây là tùy chọn tài khoản; sidebar và scroll thuộc từng thiết bị.
Mobile có thể dùng lại các giá trị này và ánh xạ theme sang native.

## Description

Migration mới thêm cột nullable vào sports, sport_formats, permissions,
listings và notification_configs. Team/Role/Report giữ cột hiện có.
Dữ liệu cũ giữ nguyên và Description mới là null.

Domain chuẩn hóa trim và trắng thành null trước mutation:
mỗi trường Description tối đa 200 ký tự, gồm môn/thể thức/quyền/vai trò/đội/kèo/
cấu hình thông báo/báo cáo. API validation và cột database dùng cùng giới hạn.
Frontend giới hạn nhập mô tả môn/thể thức và hiển thị bộ đếm 200 ký tự.
Migration `LimitDescriptionsTo200Characters` dừng nếu dữ liệu cũ vượt 200 ký tự;
cần chỉnh sửa nội dung đó có chủ ý trước khi triển khai, không tự cắt hoặc xóa.
API write và DTO read truyền Description cho môn/thể thức, kèo, cấu hình.
Permission là catalog do seed quản lý, có Create/UpdateDescription trong Domain
và DTO đọc; không thêm endpoint ghi permission ngoài phạm vi quyền hiện có.
Description của config chỉ dùng nội bộ, không nằm trong snapshot nội dung người nhận.
Không thay Note, ContactNote, cancel reason hoặc Body bằng Description.

## Kiểm thử

WebFoundationTests kiểm tra invariant/atomic mutation, actor, commit, stale token
và EF model. WebAuthHttpTests kiểm tra CSRF, cookie, JSON không lộ refresh token,
nguồn token cookie và logout.
WebFoundationPersistenceTests dùng database test riêng để kiểm tra migration giữ
dữ liệu cũ, audit sau commit và optimistic concurrency. Cần môi trường PostgreSQL
được mô tả trong testing.md; không chạy migration vào database ứng dụng để test.
