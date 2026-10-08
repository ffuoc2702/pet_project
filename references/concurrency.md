# Concurrency và shared state

Chỉ áp dụng phần này khi code có async, workers, threads hoặc state/resource dùng chung. Không thêm locks hay distributed infrastructure cho code không có nhu cầu.

## Xác định ownership

Liệt kê state bị chia sẻ, ai đọc/ghi, lifetime và invariant cần giữ. Ưu tiên immutable data, local state hoặc ownership riêng khi phù hợp. Không đưa input vào field dùng chung để giảm tham số; việc đó có thể phá reentrancy.

Tách orchestration/synchronization khỏi phần computation khi giúp hiểu và test, giữ luồng chính đủ gần để theo dõi. Không tách thành nhiều tầng chỉ vì hàm là async.

## Chọn cơ chế có hiệu lực

Dùng primitive và concurrency abstraction của runtime hoặc framework hiện có. Đánh giá check-then-act và các thao tác nhiều bước; collection thread-safe không bảo đảm chuỗi thao tác là atomic.

Giữ critical section đủ ngắn nhưng bao phủ toàn invariant. Không tách transaction hoặc lock chỉ để hàm ngắn hơn. Hạn chế I/O trong lock khi có thiết kế khác đúng đắn; không tự thay đổi consistency contract.

Phân biệt phạm vi bảo vệ: in-process lock không bảo vệ nhiều process hoặc nhiều instance. Khi invariant nằm ở DB, cân nhắc transaction, unique constraint hoặc optimistic concurrency theo contract và cấu trúc hiện có. Chọn cơ chế theo lỗi thực tế có thể xảy ra, không dựa vào giả định chỉ có một người dùng.

Giữ cancellation, cleanup, thứ tự, timeout và lifecycle của task. Không dùng fire-and-forget khi không có owner xử lý lỗi và shutdown. Cân nhắc bounded queues/backpressure khi tốc độ sản xuất vượt xử lý; chỉ thiết kế trong phạm vi yêu cầu.

## Kiểm chứng

Ưu tiên kiểm thử interleaving có kiểm soát bằng barrier/event hoặc primitive phù hợp. Kiểm tra invariant, duplicate execution hoặc lost update khi liên quan. Stress test có thể tìm lỗi nhưng một lần chạy pass không chứng minh không có race.

Không tối ưu throughput hoặc thay lock/collection mà thiếu giả thuyết và đo lường thích hợp.
