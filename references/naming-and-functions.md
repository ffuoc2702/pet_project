# Tên, hàm và comments

## Đặt tên

- Dùng thuật ngữ domain và vocabulary hiện có. Phân biệt `requestedRoles`, `assignedRoles`, `effectivePermissions` khi vai trò khác nhau; tránh `data1`, `data2`.
- Nêu đơn vị hoặc ý nghĩa khi dễ nhầm: `timeoutSeconds`, `amountInCents`, `expiresAtUtc`. Không đổi kiểu dữ liệu chỉ để khớp tên.
- Chọn tên theo độ rộng scope. Cho phép `i` trong vòng lặp ngắn và ký hiệu toán học quen thuộc trong thuật toán có ngữ cảnh rõ.
- Tránh suffix mơ hồ như `Info` hoặc `Manager` khi có tên chính xác hơn; giữ chúng nếu đó là tên chuẩn có ý nghĩa trong hệ sinh thái.
- Giữ convention như `IRepository`, `Async`, `TryParse` khi repository sử dụng. Không áp lời khuyên đặt tên Java lên C#.
- Với tên public, tìm callers và kiểm tra API, reflection, binding, JSON hoặc storage trước khi đổi. Giữ alias hoặc migration khi hợp đồng yêu cầu.

## Quyết định tách hàm

Tách khi đoạn code có tên diễn đạt một ý nghĩa hữu ích, một quy tắc tái sử dụng, một trách nhiệm thay đổi riêng hoặc độ phức tạp cản trở hiểu luồng chính. Giữ inline khi chỉ chuyển vài phép gán rõ ràng sang một nơi khác mà không giảm công sức đọc.

Cho phép hàm điều phối `CreateBooking` kiểm tra dữ liệu, áp dụng quy tắc, lưu và trả kết quả như một use case. Tách thuật toán hoặc chi tiết giao tiếp làm chìm luồng chính; giữ transaction và thứ tự side effects đúng.

Dùng guard clauses nếu làm rõ điều kiện từ chối. Kiểm tra cleanup, transaction và thứ tự kiểm tra trước khi thay nesting. Không tự đổi lỗi ưu tiên khi nhiều điều kiện cùng sai.

Ví dụ tự viết, giả định các field và status bên dưới là hợp đồng hiện có:

```python
# Khó biết d và 3 mang ý nghĩa gì.
def f(d):
    return [x for x in d if x.status == 3]

# Đổi tên và đặt tên hằng số đã đủ; chưa cần class hoặc helper.
APPROVED_STATUS = 3

def approved_requests(requests):
    return [request for request in requests
            if request.status == APPROVED_STATUS]
```

Không suy ra rằng mọi literal đều cần constant. Đặt tên cho quy tắc business hoặc giá trị gây nhầm; giữ `0`, `1` và các thành phần công thức hiển nhiên khi đọc tốt hơn.

## Tham số và side effects

- Giữ phụ thuộc tường minh. Nhóm tham số thành value object/options chỉ khi chúng tạo thành một khái niệm thực sự hoặc cần validation chung; không tạo túi dữ liệu để né số tham số.
- Dùng named arguments hoặc options cho lời gọi khó hiểu. Tách boolean mode khi nó che hai thao tác độc lập; giữ flag cấu hình đơn giản có ý nghĩa rõ.
- Ưu tiên pure computation khi phù hợp. Giữ mutation cần thiết cho command, I/O hoặc hiệu năng đã xác định.
- Giữ API command trả ID, kết quả tạo mới hoặc outcome khi hữu ích. Phân biệt command/query giúp hiểu side effects; không bắt buộc triển khai CQRS.

## Comments và format

Giữ comment về lý do, thuật toán khó, workaround có nguồn, invariant, tính nguyên tử hoặc giới hạn vận hành. Giữ tài liệu API và thông tin pháp lý cần thiết. Tránh comment dịch lại từng dòng hoặc khẳng định không còn đúng.

Xóa commented-out code trong vùng đang sửa nếu không còn giá trị; dùng version history khi cần truy hồi. Không xóa ví dụ tài liệu hoặc comment chứa quyết định thiết kế chỉ vì chúng giống code.

Dùng formatter hiện có. Không format toàn repository khi sửa một lỗi nhỏ.
