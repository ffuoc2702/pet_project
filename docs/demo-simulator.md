# Tool mô phỏng MatchingSports

Console C#/.NET 10 tại `backend/tools/MatchingSports.DemoSimulator`. Mặc định tạo 50 người dùng, 6 team × 5 thành viên, 20 người vãng lai và 15 kèo bóng đá 5v5. Tool phục vụ demo portfolio/API, không đo tải HTTP.

## Chạy nhanh

Bật container `matchingsports-postgres-dev`, chạy từ `backend/`:

```powershell
.\scripts\Demo.ps1 -RunId cv-demo-01
```

Script lấy kết nối từ Docker và hỏi mật khẩu chung bằng secure prompt. Mật khẩu phải đạt policy Identity (ít nhất 10 ký tự, hoa/thường/số/ký tự đặc biệt). Dữ liệu được giữ trong database container để đăng nhập qua API. Email dạng `demo.cv-demo-01.001@example.test`; ID/email đầy đủ trong report.json. Tài khoản giả được xác nhận email bằng seed Identity, chỉ nhận role USER.

Chạy trong database test riêng rồi tự xoá:

```powershell
.\scripts\Demo.ps1 -Mode test -RunId smoke-01
```

Test mode tạo database ngẫu nhiên `matchingsports_rbac_test_demo_*`; cần quyền tạo/xoá database và extension PostGIS. Báo cáo vẫn được giữ, nhưng tài khoản không còn sau cleanup. Mỗi lượt test là database mới.

Điều chỉnh quy mô:

```powershell
.\scripts\Demo.ps1 -Users 70 -Teams 10 -Listings 25 -RunId cv-demo-02
```

Users: 16–200; teams: 2–20; cần ít nhất `teams × 5 + 6` người; listings: 8–50. RunId: 1–32 ký tự chữ thường, số, dấu gạch ngang. Dùng `-DatabaseContainer <tên>` để đổi container; `-NoBuild` sau khi build Release.

## Cấu hình

Tool đọc environment hoặc user-secrets của project riêng: `ConnectionStrings__matchingsports`, `Demo__Password` (user-secrets: `ConnectionStrings:matchingsports`, `Demo:Password`). Không truyền password trên command line hoặc ghi vào repo. Dùng `Demo.ps1 -UseUserSecrets` để bỏ đọc Docker và secure prompt khi đã cấu hình đủ secrets/environment.

```powershell
dotnet run --project tools/MatchingSports.DemoSimulator --configuration Release -- --run-id cv-demo-01 --mode demo
dotnet run --project tools/MatchingSports.DemoSimulator --configuration Release -- --help
```

Chỉ nhận host PostgreSQL local: localhost, 127.0.0.1, ::1. Tool apply migration và seed catalog/permission trước khi chạy. Có thể cấu hình database demo riêng; không cần API chạy. Tránh sửa/xoá resource demo trong lúc mô phỏng.

## Kịch bản

1. Seed qua UserManager, hash mật khẩu/gán USER; cập nhật hồ sơ/môn chơi qua use case.
2. Tạo team, mời/nhận lời mời, uỷ quyền đội phó.
3. Tạo kèo gần/xa; tìm theo bán kính và giờ bắt đầu.
4. Người vãng lai/team đăng ký; chủ kèo duyệt, xác nhận hoặc huỷ trận.
5. Từ chối request, rút request, huỷ kèo còn request pending.
6. Tạo trận trong cửa sổ nhắc lịch cấu hình, chạy job và kiểm tra history.
7. Tìm khi chưa có kèo, tạo watch, tạo kèo phù hợp; chạy worker hai lượt để kiểm tra chỉ một delivery.
8. Đọc hồ sơ công khai của người khác/team và inbox có thông báo.

Action nghiệp vụ gọi MediatR với actor đã kiểm tra mật khẩu, permission pipeline/audit hiện có. Seed có actor null. Tool không tự ghi đè trạng thái entity hoặc reset mật khẩu. Dùng in-app, không chạy SMTP dispatcher. Matching/reminder jobs xử lý queue/trận đến hạn của **toàn database**; dùng database demo riêng khi cần cô lập. Tool không đổi system settings.

## Checkpoint và báo cáo

Mặc định: `backend/.artifacts/demo-simulator/<database-key>/<RunId>/`.

- `state.json`: resource IDs, lịch cố định, bước hoàn tất và activeStep.
- `report.json`: users/team/listings, trạng thái, số history/delivery.
- `run.lock`: khoá file; advisory lock PostgreSQL ngăn simulator chạy đồng thời.

Không chứa password/token/chuỗi kết nối. Đổi output bằng `--output <directory>` khi chạy trực tiếp.

Chạy lại cùng database/RunId/tham số/mật khẩu sẽ tái sử dụng dữ liệu. Checkpoint hoàn tất trả báo cáo cũ; không chạy lại action. Sau gián đoạn, tool đọc trạng thái/token hiện tại để tiếp tục và không ghi đè conflict. Giữ checkpoint/mật khẩu để resume. Nếu dữ liệu bị sửa/xoá ngoài kịch bản, quyền USER bị thu hồi hoặc thời gian đã quá hạn, tool có thể dừng; kiểm tra rồi dùng RunId/database demo mới. Không có lệnh xoá hàng loạt dữ liệu demo.

## Kiểm thử

`DemoSimulatorTests`: CLI sai, toàn kịch bản, chạy lại không tăng bản ghi, resume sau action đã commit, từ chối mật khẩu thay đổi, audit team và cleanup test DB. Chạy bằng `backend/scripts/Test.ps1`.
