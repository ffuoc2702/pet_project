# Thiết kế và ranh giới phụ thuộc

## SRP và cohesion

Xác định trách nhiệm theo **lý do thay đổi**, không theo số method hoặc số động từ. Tách khi thay đổi một policy gây tác động không cần thiết đến policy khác, hoặc một nhóm state/method phục vụ mục đích độc lập. Giữ những bước của cùng use case gần nhau khi việc tách làm khó theo dõi.

Đặt invariant tại nơi sở hữu trạng thái và ngăn chuyển trạng thái không hợp lệ. Dùng encapsulation cho domain có hành vi; cho phép record, DTO và cấu trúc dữ liệu đơn giản tại ranh giới truyền dữ liệu. Không thêm getters/setters hoặc domain model phức tạp cho mọi DTO.

Không dùng inheritance chỉ để chia sẻ vài dòng. Ưu tiên composition khi quan hệ thay thế không rõ; giữ kế thừa phù hợp với framework hoặc một abstraction có hợp đồng ổn định.

## Kiểm tra trước khi thêm abstraction

Trả lời bằng tình huống hiện tại:

1. Abstraction này che chi tiết biến động nào, bảo vệ invariant nào hoặc giúp kiểm chứng gì?
2. Callers có dùng vocabulary của domain thay vì chỉ lặp lại API bên dưới không?
3. Nó có loại bỏ một quy tắc trùng lặp thực sự không?
4. Chi phí file, wiring, navigation và debugging có hợp lý không?

Nếu chưa có lợi ích cụ thể, giữ implementation trực tiếp. Một interface có một implementation vẫn hợp lý khi cần ranh giới I/O hoặc thay thế deterministic trong test; nhiều implementation cũng không tự động chứng minh abstraction tốt.

## Duplication

Phân biệt lặp **kiến thức** với lặp **hình thức**. Hai hàm hiện giống nhau nhưng policy thay đổi độc lập không nhất thiết nên hợp nhất. Hai nơi tính cùng một quy tắc phí giao dịch nên có một nguồn quy tắc nếu giảm nguy cơ lệch nhau.

AVOID helper generic nhận nhiều flags hoặc callbacks chỉ để gom vài dòng. Chọn extraction theo domain, có input/output rõ. Cho phép một ít duplication khi abstraction chưa có tên và hợp đồng ổn định; không áp quy tắc máy móc về số lần xuất hiện.

## Ranh giới I/O

Tách phần quyết định nghiệp vụ khỏi clock, randomness, network, filesystem hoặc persistence khi các phụ thuộc này cản trở kiểm chứng hoặc làm code khó thay đổi. Tận dụng seam/framework sẵn có trước khi thêm wrapper.

Bọc thư viện khi cần policy chung về lỗi, retries, auth hoặc chuyển đổi domain. Không bọc API ổn định bằng lớp pass-through chỉ vì thư viện là bên thứ ba. Dùng integration/contract tests cho giả định về ranh giới.

Giữ construction/configuration tại composition root khi project đã có DI. Không đưa service locator hoặc DI container mới vào helper đơn giản.

Cho phép switch/pattern matching cho tập variant nhỏ, ổn định và rõ ràng. Cân nhắc polymorphism khi cùng một dispatch bị lặp ở nhiều nơi và biến thể có hành vi mở rộng độc lập.

## Refactor an toàn

Trước khi thay đổi data access, kiểm tra query translation, lazy execution, số lần gọi DB, ordering, pagination, transaction và cancellation. Không đổi collection hoặc materialize sớm vì hình thức đẹp hơn.

Không bắt callers biết sâu cấu trúc nội bộ nếu một thao tác domain có thể diễn đạt ý định tốt hơn. Cho phép truy cập DTO hoặc fluent API hợp lý; không xem mọi chuỗi dấu chấm là vi phạm.
