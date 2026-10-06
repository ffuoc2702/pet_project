# Convention command

Chọn use case hiện có phù hợp cùng loại thay đổi. Với use case ghi tiêu chuẩn
có người dùng đã xác thực, `UpdateUserProfile` là implementation tham chiếu hiện tại.
Convention được xác định bởi các quy tắc kiến trúc; đối chiếu implementation với
các quy tắc này trước khi áp dụng cho feature khác.

## Implementation tham chiếu: UpdateUserProfile

Đọc các file sau trước khi xây use case ghi tương tự:

- `backend/src/MatchingSports.Api/Controllers/AuthController.cs`
- `backend/src/MatchingSports.Api/Contracts/UserProfile/UpdateUserProfileRequest.cs`
- `backend/src/MatchingSports.Application/Features/UserProfile/Update/UpdateUserProfileCommand.cs`
- `backend/src/MatchingSports.Application/Abstractions/UserProfile/IUserProfileRepository.cs`
- `backend/src/MatchingSports.Domain/Entities/UserProfile.cs`
- `backend/src/MatchingSports.Infrastructure/Persistence/Repositories/UserProfileRepository.cs`
- `backend/tests/MatchingSports.Tests/UpdateUserProfileTests.cs`

Luồng chuẩn:

```text
HTTP request + authenticated user ID
  -> Controller: tạo command, mediator.Send(command, cancellationToken)
  -> MediatR pipeline: kiểm tra permission nếu request triển khai IPermissionAuthorizedRequest
  -> Handler: lấy user ID qua ICurrentUser.UserId
  -> Handler: repository.GetForUpdateAsync(userId, cancellationToken)
  -> Handler: xử lý không tìm thấy / quyền truy cập
  -> Domain: entity.UpdateDetails(...) hoặc hành vi nghiệp vụ tương ứng
  -> Handler: unitOfWork.SaveChangesAsync(cancellationToken)
  -> Handler: map entity thành DTO
  -> Controller: OkResponse(result)
```

Command user profile chỉ chứa dữ liệu cần cập nhật, không chứa UserId.
Handler lấy UserId qua ICurrentUser; implementation ở API đọc claims đã xác thực.
Không lấy danh tính người thực hiện từ request body.
`GetForUpdateAsync` trả entity tracked; đây không phải lời hứa khóa hàng database.
Query chỉ đọc nên dùng projection hoặc `AsNoTracking()` trong Infrastructure.
Repository không tự gọi SaveChanges; handler điều phối commit qua `IUnitOfWork`.

### Controller và handler

- Request API dùng DataAnnotations theo convention hiện có; command dùng
  `sealed record ... : IRequest<TResult>`; handler dùng `IRequestHandler<...>`.
- Controller chỉ xử lý giao thức. Handler chỉ điều phối use case, không chứa
  business rule hoặc thao tác chuẩn hóa entity thay cho Domain.
- Truyền CancellationToken xuyên suốt các lời gọi async hỗ trợ cancellation.
- Dùng DTO để trả kết quả; không trả entity EF/navigation ra API.
- Entity -> DTO dùng ApplicationMappingProfile; DTO -> response riêng dùng
  ContractMappingProfile khi cần. Handler đã trả DTO thì controller không map lại
  cùng kiểu chỉ để sao chép dữ liệu.
- Success dùng `BaseResponse<T>`; danh sách phân trang dùng convention
  `PaginatedResponse<T>` hiện có. Khai báo response type đúng payload thực tế.
- Dùng exception handling tập trung và Problem Details. Không catch chỉ để log
  rồi throw lại; tránh log cùng lỗi ở nhiều tầng. Catch chỉ khi có recovery,
  chuyển lỗi kỹ thuật hoặc cleanup thực sự cần thiết.
- Mã lỗi ổn định dùng `MatchingSports.Shared.ErrorCodes`; không hard-code chuỗi
  như `"INVALID_ACCESS_TOKEN"`. Constants dùng chung có tên rõ nghĩa và được
  nhóm theo mục đích; không biến mọi literal cục bộ thành constant.

## Tên và tổ chức code

- Tên phải diễn đạt hành động, đối tượng và mục đích. Tránh `Process`, `DoWork`,
  `ExecuteLogic`, `Helper`, `Manager` khi tên không cho biết hành vi cụ thể.
- Ví dụ: `GetForUpdateAsync`, `UpdateUserProfile`, `UpdateDetails`,
  `SendEmailVerificationOtpAsync`, `RevokeRefreshSessionAsync`.
- Phương thức async tự định nghĩa có hậu tố Async. Giữ tên bắt buộc của framework
  như MediatR `Handle` và theo convention controller hiện có.
- PascalCase cho type/member, camelCase cho parameter/local. Boolean thể hiện
  điều kiện như `IsActive`, `HasAvailableCapacity`, `CanAcceptRequest`.
- Không đổi `StringNormalizer.StartEndSpaceTrim` hoặc các public API đang dùng
  chỉ vì sở thích đặt tên; rename phải có lý do và cập nhật toàn bộ call site.
- File và thư mục theo feature hiện tại:

```text
Api/Contracts/<Feature>/<Action>Request.cs
Application/Features/<Feature>/<Action>/<Action>Command.cs
Application/Features/<Feature>/Dtos/<Feature>Dto.cs
Application/Abstractions/<Feature>/I<Feature>Repository.cs
Domain/Entities/<Entity>.cs
Infrastructure/Persistence/Repositories/<Feature>Repository.cs
```

- Namespace theo file lân cận: `MatchingSports.Api.Contracts`,
  `MatchingSports.Application.Features.<Feature>`,
  `MatchingSports.Application.Abstractions`, `MatchingSports.Domain.Entities`,
  `MatchingSports.Infrastructure.Persistence.Repositories`, `MatchingSports.Shared`.
- Command và handler có thể cùng file theo mẫu UpdateUserProfile hiện tại.
  Tách khi kích thước/độ phức tạp cần thiết; không tái tổ chức toàn repo cho task nhỏ.
- Dùng nullable đúng nghĩa, guard clause, hàm ngắn với một trách nhiệm. Comment
  giải thích lý do hoặc ràng buộc khó thấy; không diễn giải lại code rõ nghĩa.
- Không thêm generic repository, base handler hay abstraction mới nếu chưa có
  nhu cầu cụ thể. Tái sử dụng abstraction hiện có trước.

## Tài liệu liên quan

- [Kiến trúc và hành vi Domain](../architecture.md)
- [Danh tính và phân quyền](../authorization.md)
- [Persistence](../persistence.md)
- [Kiểm thử](../testing.md)
