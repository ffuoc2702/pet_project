# MatchingSports web

Base React + TypeScript strict + Vite, tổ chức theo feature.
Áp dụng [AGENTS.md](AGENTS.md) và [SKILL.md](../SKILL.md) của repo.

Bộ UI custom trên Ant Design, tokens/theme, auth flows và quy tắc mở rộng:
[docs/ui-base.md](docs/ui-base.md). Chạy dev và mở `/dev/ui` để xem component
showcase; production không bật route này.

Màn quản trị: `/users` (người dùng), `/roles` (vai trò), `/roles/matrix`
(ma trận quyền theo role). Dùng chung cấu trúc bảng của Sports; menu và thao tác
theo permission. Chi tiết API, filter, concurrency và lưu quyền:
[docs/user-directory.md](../docs/user-directory.md).

## Chạy local

Node 22.12+, Bash (Windows dùng Git Bash) và backend .NET 10:

```bash
npm ci
cp .env.example .env.local
npm run dev
```

SERVER_HTTP mặc định trỏ tới API container ở cổng 8080; nếu dùng Aspire,
thay bằng SERVER_HTTPS theo URL API của Aspire. Proxy local chuyển /api và
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

```bash
npm run generate:api -- <path-or-url-to-openapi.json>
```

Lệnh sinh shared/api/generated/schema.d.ts để tham khảo/tái sử dụng khi thêm feature.
Không dùng schema cũ làm nguồn chính nếu chưa sinh lại từ backend đang triển khai.

## Kiểm tra

```bash
npm run typecheck
npm run check:names
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

Chạy API backend bằng Docker và cấu hình Vite proxy theo
[hướng dẫn container local](../docs/local-containers.md).

Quy tắc tên file áp dụng toàn bộ frontend: kebab-case, kể cả component và hook.
Tên định danh trong code: component PascalCase, hook camelCase với tiền tố use.
Ví dụ `navigation-link.tsx` export `NavigationLink`, `use-debounce.ts` export
`useDebounce`. `check:names` kiểm tra cả casing import/export tĩnh và dynamic
imports. AGENTS.md/README.md và suffix chuẩn như .config.ts/.test.ts/.d.ts giữ
convention của công cụ. Dependency và output sinh tự động không thuộc phạm vi
đổi tên.

## Metadata và cấu hình động

Route registry tại `src/shared/config/routes.ts` là nguồn duy nhất cho đường dẫn,
nhãn i18n, permission, parent và metadata breadcrumb. Dùng `routePath()` cho link
và redirect, không ghép URL trong page. App chỉ lắp ghép registry với router.

Route có nhãn theo bản ghi khai báo `resourceLabel: { type, parameter }`. Feature
lấy dữ liệu bằng query hiện có và gọi `useResourceLabel(type, id, data?.name)`.
Breadcrumb/tiêu đề đọc nhãn theo đúng type và ID, dùng nhãn i18n trong khi chưa có
dữ liệu; nhãn được cập nhật khi dữ liệu thay đổi và gỡ khi page unmount. Navigation
không gọi API nghiệp vụ hoặc phân nhánh theo tên feature. Tiêu đề trang ghép chuỗi
breadcrumb; trang auth dùng tiêu đề riêng từ i18n.

Sidebar tree nhận icon dạng ReactNode; renderer không có danh sách icon cố định.
Tên ứng dụng dùng `VITE_APP_NAME`; tọa độ ban đầu dùng `VITE_MAP_DEFAULT_LATITUDE`
và `VITE_MAP_DEFAULT_LONGITUDE`. Theme dùng trạng thái dark đã được provider phân
giải; danh sách preset lấy từ registry palette. Cấu hình build-time vẫn có mặc
định tại `shared/config/env.ts`, cần build/restart Vite sau khi đổi env.

## Loading và dữ liệu trước khi chuyển trang

`AppLoading` là loading chung gồm ba chấm lượn sóng, dùng màu semantic và hỗ trợ
`prefers-reduced-motion`. Button/table/card/bản đồ và route fallback dùng cùng
component, không dùng spinner/skeleton riêng.

Router giữ màn hiện tại dưới loading fullscreen khi module hoặc loader của màn
mới chưa hoàn tất. `createRouteLoader` chờ session, permission và preferences;
feature đăng ký loader để chờ tất cả query cần cho form/page trong `prepareRoutes`.
Sports dùng chung query options giữa loader và page để dữ liệu nằm trong cache
trước khi form mount. Nếu cache còn mới và đủ dữ liệu, chuyển trang ngay.

Khi thêm màn có nhiều nguồn dữ liệu, loader phải await tất cả bằng `Promise.all`
và khai báo query key/options dùng chung với page. Không tải dữ liệu khởi tạo form
chỉ trong `useEffect` sau khi chuyển màn. Form tạo mới dùng default đồng bộ không
cần chờ API. Google Maps là tích hợp tùy chọn; tọa độ form có sẵn trước khi SDK tải.

Navigation bị hủy ngừng chờ ngay, không hủy query mà route khác đang dùng chung;
queryFn vẫn nhận AbortSignal của TanStack Query. API thất bại chuyển tới error
boundary có mã lỗi/đường dẫn quay về, không giữ loading vô hạn hoặc hiện form trống.

Trang sports dùng toolbar tìm kiếm (50% chiều ngang desktop), dropdown trạng thái
và bảng được bọc trong `AppCard` dùng class `app-table-card`. `status=active|inactive` nằm trong URL/cache key và gửi
`isActive=true|false` tới API; trạng thái tất cả không gửi `isActive`.
`AppTable` nhận `borders={{ horizontal, vertical }}` để cấu hình từng hướng độc lập; cấu hình viền không ảnh hưởng API.
Nội dung text/link của ô bảng mặc định tối đa 3 dòng, vượt quá hiện `…` và
tooltip đầy đủ. Cấu hình chung qua `maxCellLines`, riêng từng cột qua `maxLines`.

Bố cục list dùng class `app-table-card` và `AppTableToolbar` từ
`shared/ui/table-toolbar.tsx`, nhận các vùng `search`, `filters`, `options`.
CSS responsive, bố cục body Card và
đường viền nằm trong `shared/ui/table.css`, không khai báo selector theo feature.

Cột trạng thái sports dùng `SportStatusSwitch` trên base `AppSwitch`.
Bật gọi POST `/sports/{id}/activate` (sports.update), tắt gọi POST
`/sports/{id}/deactivate` (sports.deactivate). Pending dùng overlay loading chung
của root navigation, không hiện loading trong switch/button/table;
không đổi checked trước khi API thành công, không retry mutation tự động.
Sau thành công cập nhật cache detail và invalidate list để giữ đúng filter/phân trang.
