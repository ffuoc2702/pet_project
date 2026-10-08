# Xử lý lỗi và dữ liệu vắng mặt

## Chọn representation phù hợp

Giữ contract và convention của project. Dùng exception cho thất bại theo cơ chế của ngôn ngữ/framework; dùng Result/Option/error return cho expected outcomes khi hệ sinh thái hỗ trợ. Không bắt mọi ngôn ngữ theo mô hình exception của Java.

Phân biệt dữ liệu không tồn tại, dữ liệu không hợp lệ, thiếu quyền, lỗi dependency và lỗi lập trình. Không biến tất cả thành `false`, empty collection hoặc cùng một HTTP status nếu callers cần phản ứng khác nhau.

Bổ sung context về operation và định danh an toàn, giữ nguyên cause/stack. Không đưa password, token hoặc dữ liệu nhạy cảm vào message/log. Chuyển lỗi tại boundary có trách nhiệm dịch hợp đồng; tránh wrap cùng một lỗi ở mọi tầng.

## Catch đúng chỗ

Catch khi có thể phục hồi, bổ sung context có giá trị, bảo đảm cleanup hoặc chuyển đổi sang hợp đồng tầng ngoài. Để lỗi propagate khi không có hành động hữu ích.

AVOID catch-all rồi trả `[]`, `null`, `0` hoặc `success`. Nếu hệ thống chủ ý cho phép degraded mode, thực hiện policy đó tường minh và kiểm chứng failure path.

Giữ cancellation theo convention runtime; không coi hủy tác vụ là lý do tự động retry. Khi cần retry, dùng policy hiện có và kiểm tra transient failure, giới hạn lần thử, thời gian và tính idempotent. Không thêm retry chỉ vì có exception.

Sử dụng `finally`, context manager, `using` hoặc cơ chế cleanup chuẩn cho resource ownership. Không để cleanup làm mất nguyên nhân lỗi ban đầu ngoài policy được xác định.

## Null, Option và empty

Với API mới, trả collection rỗng khi ý nghĩa là truy vấn thành công nhưng không có phần tử. Không dùng collection rỗng khi ý nghĩa là truy vấn thất bại hoặc dữ liệu chưa được tải.

Cho phép nullable/Option biểu diễn absence có ý nghĩa. Xác định rõ caller cần branch, cung cấp default hay báo lỗi. Không tạo Null Object chỉ để bỏ một `if`; dùng khi default behavior hợp lệ theo domain.

Với API hiện có, không đổi `null` thành `[]` hoặc exception trong một cleanup nếu callers phân biệt các trạng thái này. Nếu nhiệm vụ yêu cầu đổi contract, cập nhật callers và tests tương ứng.

Validate tại trust boundary và nơi cần bảo vệ invariant. Không lặp kiểm tra khắp mọi private method khi invariants đã được bảo đảm; không loại bỏ authorization hoặc validation tại boundary.

## Kiểm chứng

Đối chiếu success path, expected failure, dependency failure và resource cleanup phù hợp với thay đổi. Kiểm tra error type/code, nội dung an toàn và cause khi chúng là phần hợp đồng; không assert toàn bộ wording lỗi nếu không cần.
