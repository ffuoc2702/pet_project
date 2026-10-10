# MatchingSports web

Base React + TypeScript strict + Vite, tổ chức theo feature.
Áp dụng [AGENTS.md](AGENTS.md) và [SKILL.md](../SKILL.md) của repo.

## Chạy local

Node 22.12+ và backend .NET 10:

```powershell
npm ci
Copy-Item .env.example .env.local
npm run dev
```

Sửa SERVER_HTTPS theo URL API của Aspire. Proxy local chuyển /api và
/signin-google tới backend, giữ Host của web. Kiểm tra certificate backend
bị tắt chỉ trong development; production dùng HTTPS có certificate hợp lệ.
Với Google OAuth local, đặt DEV_HTTPS=true, tin certificate local của Vite,
và đăng ký redirect URI https://<web-host>:<port>/signin-google trên Google Cloud.

Biến VITE_* là cấu hình public được đóng vào bundle lúc build, không chứa
secret server. Google Maps cần cả VITE_GOOGLE_MAPS_API_KEY và
VITE_GOOGLE_MAPS_MAP_ID. Key giới hạn HTTP referrers và API được phép; dùng
key riêng dev/production/mobile. Chỉ bật Maps JavaScript API và API thực sự
dùng; đặt quota và cảnh báo ngân sách. Không có key vẫn nhập tọa độ thủ công.

## Kiến trúc

- app: composition, providers, router, layouts và navigation.
- features: auth, preferences, sports, location. Mỗi feature sở hữu API,
  types, query keys, form và component nghiệp vụ; export công khai qua index.ts.
- shared: HTTP client, UI thuần, i18n, semantic theme tokens, hooks và SDK integration.
- Dữ liệu server nằm trong TanStack Query; UI/form state gần nơi dùng.
- Ant Design cung cấp component/theme, Bootstrap chỉ cung cấp grid/container.
- Routes và bản đồ tải theo nhu cầu. Google Maps listeners được cleanup;
  tests không gọi Google hoặc phát sinh chi phí.

## Sidebar và điều hướng

Khai báo route tại src/app/navigation/routes.ts; router ánh xạ route ID tới
màn lazy tại src/app/router/router.tsx. Cấu hình menu bằng sidebarTree tại
src/app/navigation/sidebar.ts. Ví dụ:

```ts
{ id: 'explore', labelKey: 'common:explore', children: [
  { id: 'sports', routeId: 'sports', icon: 'sports' },
  { id: 'location', routeId: 'location', icon: 'location' },
] }
```

Tree hỗ trợ nhiều cấp, đổi thứ tự, nhãn dịch và icon. permission khai báo ở
route registry; menu bỏ các nhánh không còn mục được phép. Backend vẫn kiểm
tra quyền mỗi request. Đây là cấu hình bằng code, chưa có trình sửa menu admin.

Breadcrumb đi theo parent của route, tên chi tiết lấy từ query cache.
Back/Forward theo lịch sử nội bộ; mở URL trực tiếp có Back về route cha.
Filter/pagination nằm trong URL, scroll được router khôi phục.
Previous/Next của wizard cần khai báo theo từng luồng nghiệp vụ khi thêm wizard.

## Ngôn ngữ, nền và bộ màu

vi/en tại src/shared/i18n; mọi key có ở cả hai ngôn ngữ.
Dùng Intl định dạng ngày/số; nội dung người dùng được giữ nguyên.
Bản dịch dùng namespace common/auth/preferences/location/errors/validation;
key trong code dùng cú pháp namespace:key. Tách thành file nhỏ theo namespace khi cần.

Chế độ light/dark/system độc lập preset blue/green/purple.
Semantic tokens áp dụng cả Modal, Drawer, Table và notification.
Script public/preferences-init.js áp dụng ngôn ngữ/nền đã lưu trước React.
Tùy chọn khách lưu local; tài khoản đọc/lưu qua /api/v1/me/preferences.
Save đồng bộ có concurrency token. 409 yêu cầu Reload và gửi lại có chủ ý;
không tự ghi đè. Trạng thái sidebar chỉ lưu theo thiết bị.

## API và auth

HTTP client tại shared/api: AbortSignal, Problem Details, correlation ID.
Chỉ retry một lần request đọc sau refresh; không tự replay mutation.
Query retry tối đa một lần với lỗi mạng/5xx; không retry 4xx.
Search dùng debounce có cancel/flush; Enter tìm ngay.

Web dùng /api/v1/auth/web/csrf, /login, /refresh, /logout và /google.
Access token chỉ trong memory. Refresh token trong cookie HttpOnly:
Secure + SameSite=Lax trên production, không xuất hiện trong JSON của web.
Request auth ghi có header X-CSRF-TOKEN; logout chỉ xóa state khi server thành công.
Refresh được gom trong tab và tuần tự giữa các tab qua Web Locks khi trình
duyệt hỗ trợ; logout thông báo qua BroadcastChannel.
Khi Web Locks không được hỗ trợ, conflict được hiển thị để đăng nhập lại.

Contract token /api/v1/auth hiện có được giữ cho mobile.
Production phục vụ web và API trên cùng origin; cấu hình cross-site chưa được hỗ trợ.
Google callback redirect cố định /auth/callback, không truyền token trong URL.

Contract frontend cho các feature mẫu tại types.ts/shared/api/contracts.ts.
Có thể kiểm tra contract bằng OpenAPI backend:

```powershell
npm run generate:api -- <path-or-url-to-openapi.json>
```

Lệnh sinh shared/api/generated/schema.d.ts để tham khảo/tái sử dụng khi thêm feature.
Không dùng schema cũ làm nguồn chính nếu chưa sinh lại từ backend đang triển khai.

## Kiểm tra

```powershell
npm run typecheck
npm run lint
npm run format:check
npm test
npm run build
npm run browser:install
npm run test:e2e
```

Browser cache mặc định ở frontend/.cache/ms-playwright; có thể đổi qua
PLAYWRIGHT_BROWSERS_PATH.

Unit tests kiểm tra refresh đồng thời, mutation không replay, debounce,
sidebar permission và translation/storage. E2E mock API để kiểm tra navigation,
preferences, responsive drawer, tọa độ thủ công và form chưa lưu.

## Deploy

1. Chạy migration mới WebFoundationPreferencesAndDescriptions bằng migrator của backend.
2. Cấu hình API/auth/Google OAuth bằng environment hoặc user-secrets phù hợp.
3. Build web với cấu hình Google Maps public của môi trường đích.
4. Phục vụ dist qua web server/reverse proxy, proxy /api và /signin-google tới API.
5. SPA fallback về index.html cho route web; không fallback endpoint /api bị thiếu.
6. Cấu hình DataProtection__KeyDirectory trên API tới volume key ring bền vững,
   dùng chung giữa các replica; bảo vệ volume bằng quyền filesystem và mã hóa của nền tảng.
7. index.html và preferences-init.js phải revalidate; assets có hash được cache immutable.
8. Dùng HTTPS, security headers và CSP cho phép các origin Google SDK thực sự dùng.

CI chạy frontend checks/E2E và backend checks/PostgreSQL integration tests.
Mobile sau dùng lại API, mã lỗi và preferences, còn navigation và SDK map dùng native.
