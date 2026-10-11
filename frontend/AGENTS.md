# MatchingSports frontend

Kế thừa hướng dẫn tại ../AGENTS.md và áp dụng ../SKILL.md.

- Tên file frontend dùng `kebab-case`, kể cả file component và hook:
  `navigation-link.tsx`, `use-debounce.ts`, `query-client.ts`. Đây là quy tắc
  tên file, độc lập tên định danh trong code: component/type dùng `PascalCase`
  (`NavigationLink`), hook dùng `camelCase` với tiền tố `use` (`useDebounce`).
  Suffix chuẩn như `.config.ts`, `.test.tsx`, `.d.ts` và tên metadata chuẩn
  `AGENTS.md`/`README.md` giữ theo công cụ; dependency/output sinh tự động không
  thuộc phạm vi đổi tên. Import/export phải khớp casing thật của file, kể cả
  trên Windows. `npm run check:names` kiểm tra toàn bộ file frontend tự quản lý
  và các import local; lệnh lint cũng chạy kiểm tra này.
- `app` lắp ghép providers, routes, navigation và layouts. Feature sở hữu API,
  query keys, types, form và UI nghiệp vụ; public exports tại `index.ts`.
- `shared` không import `features` hoặc `app`. Không import sâu feature khác.
  Chỉ tạo abstraction/thư mục khi có nơi sử dụng cụ thể.
- Component không gọi fetch trực tiếp. Dùng HTTP client và TanStack Query;
  truyền AbortSignal, giữ pagination/filter/sort trong URL và không tải toàn bộ list.
- Route registry tại `shared/config/routes.ts` là nguồn route ID, đường dẫn,
  permission và breadcrumb; feature dùng `routePath()` cho link/redirect. Nhãn
  bản ghi khai báo `resourceLabel` và cung cấp qua `useResourceLabel`; navigation
  không gọi API nghiệp vụ hoặc phân nhánh theo feature. Sidebar tree cấu hình
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
- Dùng custom UI từ `@/shared/ui` hoặc public module tương ứng
  (`buttons`, `forms`, `controls`, `table`, `overlays`, `data-display`) cho
  controls/form/table/popup; không import
  các control đã được bọc trực tiếp từ Ant Design. Provider và layout/navigation
  primitives là ngoại lệ được mô tả trong [docs/ui-base.md](docs/ui-base.md).
- Màu semantic/component tokens nằm ở `shared/theme/tokens.ts`; preset có seed
  riêng cho nền tối. Toast/notification/confirm dùng `useAppFeedback`, không gọi
  API static của Ant làm mất context. Xác thực dùng `AuthLayout` và API hiện có;
  password/OTP không lưu storage, URL hoặc router state.
- Tests tập trung hành vi/rủi ro, mock Google SDK và không phát sinh phí provider.

Chạy trong frontend: `npm ci`, `npm run typecheck`, `npm run lint`,
`npm run format:check`, `npm test`, `npm run build`, `npm run test:e2e`.
Không báo pass khi chưa chạy; ghi rõ lý do kiểm tra bị chặn.

- Loading chỉ dùng `AppLoading`/`AppLoadingBoundary` từ shared UI. Route có dữ liệu
  khởi tạo form/page phải đăng ký loader tại `prepareRoutes`, dùng query options
  chung với page và await đủ các nguồn trước khi router commit màn mới. Không
  thay màn rồi fetch dữ liệu để điền form; giữ màn hiện tại dưới loading trong khi
  chờ. Không nuốt lỗi loader hoặc lấy stale response cho route đã bị hủy.
