# Backend container cho frontend local

Yêu cầu Docker Desktop chạy Linux containers, Bash, Python 3, curl (Windows dùng
Git Bash) và container
`matchingsports-postgres-dev` đang chạy, có publish cổng PostgreSQL ra host.

Từ thư mục repository:

```bash
bash ./backend/scripts/start-container.sh
```

Script build image .NET 10 ở cấu hình Release, chạy migrator trước API rồi kiểm
tra `/health`. API dùng port `8080`, chỉ bind trên `127.0.0.1`; Swagger tại
`http://127.0.0.1:8080/swagger`. Database mặc định là `matchingsports`, trên
PostgreSQL hiện có; migrator áp dụng migration và seed dữ liệu tham chiếu.
Có thể chọn database/container/port khác:

```bash
bash ./backend/scripts/start-container.sh --database-container matchingsports-postgres-dev --database-name matchingsports --api-port 8080
```

Mật khẩu PostgreSQL được lấy từ cấu hình container. Auth được lấy từ user-secrets
`matchingsports-api-identity` trong lần khởi tạo; signing key/OTP pepper còn thiếu
được sinh ngẫu nhiên. Cấu hình runtime nằm ở
`backend/.artifacts/container/appsettings.Development.json`, được Git và Docker
build context bỏ qua, bind mount read-only vào container. File được giữ qua các
lần chạy để không tự thay đổi signing key. Cập nhật file local này nếu cần đổi
SMTP/Google/auth. Không đưa file này vào source control. Script không bật seed
tài khoản admin. DataProtection keys được giữ trong named volume riêng.

Frontend dùng cấu hình local:

```dotenv
VITE_API_BASE_URL=/api/v1
SERVER_HTTP=http://127.0.0.1:8080
DEV_HTTPS=false
```

Sau đó chạy từ `frontend/`:

```bash
npm run dev
```

Browser gọi `/api/v1` trên origin frontend; Vite proxy `/api` và `/signin-google`
sang API container. Không đưa connection string hoặc secret vào `VITE_*`.
Restart Vite sau khi đổi `.env.local`. Nếu đã có `SERVER_HTTPS` trong environment
hoặc file `.env`, bỏ giá trị đó để `SERVER_HTTP` được dùng. Với Google OAuth,
bật HTTPS frontend và đăng ký đúng redirect URI với Google.

Quản lý từ `backend/`:

```bash
docker compose ps -a
docker compose logs --tail 50 api
docker compose down
```

`down` dừng API/migrator của stack này. PostgreSQL hiện có và volume DataProtection
vẫn được giữ. Khi thay code, chạy lại script để build và cập nhật container.

Compose này dùng Development để phục vụ frontend local qua HTTP.
`Http__HttpsRedirectionEnabled=false` chỉ có hiệu lực trong Development để tránh
redirect sang cổng HTTPS không tồn tại; các environment khác vẫn bật redirect.
Image runtime cài `libgssapi-krb5-2` để Npgsql có thư viện native GSSAPI cần thiết.
Khi deploy,
dùng HTTPS reverse proxy, environment Production, secret manager và cấu hình
forwarded headers cho proxy tin cậy.
