# MatchingSports frontend

Kế thừa hướng dẫn tại ../AGENTS.md và áp dụng ../SKILL.md.

- `app` lắp ghép providers, routes, navigation và layouts. Feature sở hữu API,
  query keys, types, form và UI nghiệp vụ; public exports tại `index.ts`.
- `shared` không import `features` hoặc `app`. Không import sâu feature khác.
  Chỉ tạo abstraction/thư mục khi có nơi sử dụng cụ thể.
- Component không gọi fetch trực tiếp. Dùng HTTP client và TanStack Query;
  truyền AbortSignal, giữ pagination/filter/sort trong URL và không tải toàn bộ list.
- Route registry là nguồn route ID, permission, breadcrumb; sidebar tree cấu hình
  bằng route ID. Backend quyết định quyền; ẩn menu không thay authorization.
- Access token chỉ trong memory; refresh cookie HttpOnly do backend thiết lập.
  Không log token, body auth, email hoặc vị trí chính xác. Không tự retry mutation
  hoặc ghi đè 409; yêu cầu tải lại và gửi lại có chủ ý.
- Mọi chuỗi UI dùng i18n key với cả vi/en. Nội dung người dùng không tự dịch.
  Dùng Intl cho ngày/số; không suy luận timezone từ ngôn ngữ.
- Màu UI dùng semantic tokens/CSS variables; mode light/dark/system độc lập
  preset blue/green/purple. Modal/toast phải ở trong provider theme/locale.
- Google Maps SDK nằm ở shared/integrations; nghiệp vụ vị trí thuộc location.
  Loader dùng chung, lazy load, cleanup listeners và có nhập tọa độ thủ công.
  Browser key phải giới hạn website/API; không đặt secret server vào VITE_*.
- Form có label, lỗi theo field, keyboard/focus và cảnh báo dữ liệu chưa lưu.
  Search debounce hỗ trợ Enter tìm ngay; cancellation tránh stale results.
- Tests tập trung hành vi/rủi ro, mock Google SDK và không phát sinh phí provider.

Chạy trong frontend: `npm ci`, `npm run typecheck`, `npm run lint`,
`npm run format:check`, `npm test`, `npm run build`, `npm run test:e2e`.
Không báo pass khi chưa chạy; ghi rõ lý do kiểm tra bị chặn.
