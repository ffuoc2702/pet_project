# Bộ UI dùng chung

Đọc `../AGENTS.md` và `../../SKILL.md` trước khi sửa. Public entry là
`@/shared/ui`; trang và feature dùng component custom trên Ant Design.
Các module `buttons`, `forms`, `controls`, `table`, `overlays`, `data-display`,
`use-app-feedback` cũng là public entry theo nhóm. Trang thông thường import
nhóm cần thiết để table/form không bị ghép vào chunk tải ban đầu; showcase
có thể dùng barrel tổng hợp.

## Màu và giao diện

`shared/theme/tokens.ts` giữ font, kích thước control, radius, spacing và màu
semantic cho sáng/tối. `presets.ts` giữ màu thương hiệu blue/green/purple,
có seed sáng hơn cho nền tối. `createAppTheme` ánh xạ vào Ant Design tokens
và component tokens. Link nhận cùng màu thương hiệu; button chính trên nền
tối dùng chữ tối để giữ tương phản.

`AppearanceProvider` chọn light/dark/system, nhận thay đổi hệ điều hành và
áp dụng locale. `ThemeSurface` xuất CSS variables `--surface`, `--page-bg`,
`--text`, `--muted`, `--border`, `--primary`, `--radius` cho bố cục ngoài Ant.
Không đặt màu nghiệp vụ bằng hex trong page. Popup, toast và notification
dùng context của `App` dưới `ConfigProvider`; không gọi static `Modal.confirm`
hoặc static `message` vì chúng bỏ qua context theme/locale.

Màu trạng thái success/warning/error độc lập preset. Khi đổi nền mặc định,
đồng bộ fallback `html` trong `index.css` để nền trước khi React khởi tạo khớp.

## Component

| Export                                                     | Quy chuẩn                                                                                                  |
| ---------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| AppButton / AppIconButton                                  | Button mặc định không submit; icon button bắt buộc aria-label                                              |
| AppInput / AppPasswordInput / AppSearchInput / AppTextArea | Truyền tiếp props/ref; password tối đa 128 ký tự, search có clear                                          |
| AppOtpInput                                                | 6 ô, numeric keyboard, cập nhật form cả khi xóa digit; validation OTP thuộc feature                        |
| AppSelect / AppInputNumber / AppCheckbox                   | Giữ props và value type của Ant Design                                                                     |
| AppForm / AppFormItem                                      | Vertical, focus lỗi đầu tiên, validateFirst; Item/List/ErrorList/useForm/useWatch có trên AppForm          |
| AppCard / AppAlert / AppTag / AppBadge / AppTooltip        | Token toàn cục; alert có icon, badge giới hạn 99+, tooltip delay 250ms                                     |
| AppTable<T> / AppPagination                                | Generic row, empty state dịch, size options 10/20/50/100; feature điều khiển phân trang/filter/sort server |
| AppEmpty / AppSkeleton                                     | Trạng thái rỗng dịch và skeleton active                                                                    |
| AppModal / AppConfirmDialog / AppDrawer                    | Dịch nút, quản lý focus của Ant; modal không đóng khi click mask, hủy nội dung khi đóng                    |
| PageHeader / PageContainer                                 | Title, mô tả, actions và vùng nội dung                                                                     |
| useAppFeedback                                             | Toast 3s (lỗi/cảnh báo 5s), notification lỗi giữ tới khi đóng, confirm hỗ trợ onOk async                   |

Các props mặc định có thể ghi đè khi luồng cần. Form business field/rules,
columns, row actions, permission, HTTP errors và server state thuộc feature.
Không nhúng fetch hoặc quyền của một feature vào UI base.

```tsx
import {
  AppButton,
  AppForm,
  AppFormItem,
  AppInput,
  useAppFeedback,
} from '@/shared/ui';

const [form] = AppForm.useForm<{ name: string }>();
const { message } = useAppFeedback();

<AppForm form={form} onFinish={save}>
  <AppFormItem name="name" label={t('common:name')} rules={rules}>
    <AppInput />
  </AppFormItem>
  <AppButton htmlType="submit" type="primary" loading={pending}>
    {t('common:save')}
  </AppButton>
</AppForm>;
// Chỉ thông báo thành công sau khi API thực sự hoàn tất.
await message.success(t('common:saved'));
```

ESLint chặn các control được bọc và `App` từ Ant trong app/features.
Ngoại lệ rõ ràng: provider composition; layout/navigation primitives như
Layout, Grid, Space, Breadcrumb, Menu, Avatar, Descriptions, Result, Spin và
types vẫn dùng Ant trực tiếp. Không bọc thêm abstraction khi chưa có convention
hoặc người dùng cụ thể.

## Xác thực

`AuthLayout` dùng chung cho login/register/verify-email/forgot-password/
reset-password/Google callback. Những route này không hiển thị sidebar.
Các trang dùng public UI exports, dịch vi/en và validation theo contract backend.

- Register gửi email/password/displayName; confirmPassword chỉ ở form.
- Đăng ký thành công chuyển sang OTP; chỉ email và thời gian hết hạn từ server
  đi trong router state. Password/OTP không nằm trong URL/storage/router state.
- Verify/reset hỗ trợ nhập email trực tiếp khi mở URL hoặc mất router state.
- Gửi lại có cooldown UX 60s; backend vẫn kiểm tra cooldown và rate limit thật.
  Thời gian này không thay thế cấu hình backend. API resend/reset request trả
  kết quả trung lập; UI không giả định email tồn tại hoặc mã đã được gửi thật.
- Chỉ hiện expiration khi register trả `otpExpiresAt`; resend không trả deadline
  nên bỏ deadline cũ, không tự tính thời hạn OTP giả.
- Mutation không retry. Trường hợp OTP lỗi vẫn giữ form để sửa/gửi lại chủ ý.
- Lỗi HTTP map field đã biết, nội dung lỗi dùng mã dịch, không hiện lỗi thô provider.
- Web login giữ CSRF + refresh cookie HttpOnly hiện có. Các API anonymous khác
  tái sử dụng `/api/v1/auth`; không thay đổi hợp đồng mobile/backend.

## Kiểm tra bằng trình duyệt

Chạy `npm run dev`, mở `/dev/ui` để xem màu resolved, button, field, badge,
tooltip, table, modal/drawer/toast/notification. Đổi theme/ngôn ngữ tại
`/preferences`, rồi quay lại showcase. Route và chunk showcase không được
bật trong production build.

`npm test` kiểm tra validation/contract và cơ chế nền hiện có.
`npm run test:e2e` kiểm tra auth flows, OTP xóa digit, popup dark/English,
focus, table pagination, responsive và ảnh desktop/compact. API trong E2E
được mock; kiểm thử không gửi email thật hay gọi Google. Cần cấu hình email/
Google backend để kiểm chứng provider thật trên môi trường tích hợp.

Thiết kế hiện tại theo tokens dự án; chưa đối chiếu Figma vì chưa có frame/link.
