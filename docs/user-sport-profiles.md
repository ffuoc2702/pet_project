# Hồ sơ thể thao cá nhân

Các endpoint yêu cầu đăng nhập. Danh tính người dùng lấy từ access token,
không nhận `UserId` trong request body. Thao tác ghi yêu cầu quyền
`sport_profiles.update` ở HTTP và MediatR.

| Method | Endpoint | Hành vi |
| --- | --- | --- |
| POST | `/api/v1/user-sport-profiles` | Thêm hồ sơ cho một môn |
| PUT | `/api/v1/user-sport-profiles/{profileId}` | Sửa từng hồ sơ của mình |
| DELETE | `/api/v1/user-sport-profiles/{profileId}` | Xoá từng hồ sơ và liên kết thể thức |
| GET | `/api/v1/auth/me` | Trả thông tin cá nhân kèm `data.sportProfiles` |

Body thêm mới:

```json
{
  "sportId": "<sport UUID>",
  "level": "BEGINNER",
  "preferredFormatIds": ["<format UUID>"],
  "bio": "Giới thiệu về kinh nghiệm chơi"
}
```

Body sửa có `level`, `preferredFormatIds`, `bio`; môn thể thao của bản ghi
được giữ nguyên. `preferredFormatIds` thay thế toàn bộ lựa chọn hiện tại;
`[]` xoá các lựa chọn. `bio` được trim; whitespace trở thành null.
`level` nhận `BEGINNER`, `INTERMEDIATE`, `ADVANCED` và mặc định `BEGINNER`.

Mỗi người chỉ có một hồ sơ cho mỗi môn. Môn và thể thức được chọn phải
đang hoạt động; thể thức phải thuộc môn của hồ sơ và không được trùng lặp.
Hồ sơ không tồn tại hoặc thuộc người khác trả 404. Hồ sơ trùng môn trả 409;
dữ liệu không hợp lệ trả 400 theo Problem Details.

Các thao tác ghi thành công trả 200 theo `BaseResponse`. Thêm/sửa trả DTO
của hồ sơ; xoá trả object rỗng cùng thông báo thành công.

`GET /api/v1/auth/me` giữ các field hiện tại và thêm `sportProfiles`, là
mảng rỗng nếu chưa có hồ sơ. Mỗi phần tử gồm `id`, `sportId`, `sportCode`,
`sportName`, `level`, `bio`, `createdAt`, `updatedAt`, `preferredFormats`.
Thể thức trả các field của `SportFormatDto`, bao gồm ID, mã, tên, số người
mỗi bên và trạng thái hoạt động. Đây là dữ liệu nhúng trong hồ sơ cá nhân;
không bổ sung endpoint list độc lập.
