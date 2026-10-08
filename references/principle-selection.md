# Lựa chọn nguyên tắc từ Clean Code

Dùng bảng này khi cần giải thích hoặc phân xử một lời khuyên. Đây là diễn giải chọn lọc từ bản PDF người dùng cung cấp, không phải bản sao hay tuyên bố rằng mọi điều chỉnh là quan điểm nguyên gốc của tác giả.

Nguồn: Robert C. Martin và các tác giả cộng tác, *Clean Code: A Handbook of Agile Software Craftsmanship*, bản in đầu năm 2008. Số trang bên dưới là **trang in trong sách**, không phải số trang của PDF. Tài liệu tham khảo tập trung vào chương 2-13 và 17; không chuyển mọi ví dụ Java hoặc case study thành luật cho mọi ngôn ngữ.

## Giữ và chuyển thành hành động

| Chủ đề | Nguồn trong sách | Quyết định cho agent |
| --- | --- | --- |
| Tên thể hiện ý định; searchable names | Ch. 2, tr. 18-30; Ch. 17, tr. 309-313 | Giữ; kết hợp vocabulary domain và convention repository. |
| Trách nhiệm hàm và mức trừu tượng | Ch. 3, tr. 34-39 | Giữ mục tiêu dễ hiểu; tách theo ý nghĩa thay vì quota số dòng. |
| Side effects và command/query | Ch. 3, tr. 44-46 | Giữ tính tường minh; cho phép command trả outcome hữu ích. |
| Comments có ích và comment lỗi thời | Ch. 4, tr. 53-74 | Giữ comment giải thích lý do và hợp đồng; không xem comment tự nó là thất bại. |
| Team formatting | Ch. 5, tr. 90 | Giữ convention và formatter sẵn có. |
| Encapsulation và cấu trúc dữ liệu | Ch. 6, tr. 93-101 | Giữ invariant; chấp nhận DTO và fluent API hợp lý. |
| Context lỗi; phân loại theo nhu cầu caller | Ch. 7, tr. 103-112 | Giữ; representation lỗi theo ngôn ngữ và project. |
| Ranh giới phụ thuộc | Ch. 8, tr. 113-120 | Giữ seam có mục đích; tránh wrapper pass-through không có lợi ích. |
| Test dễ đọc; một concept; F.I.R.S.T. | Ch. 9, tr. 123-133 | Giữ; test theo hành vi, không theo số assert. |
| SRP, cohesion, cô lập thay đổi | Ch. 10, tr. 136-151 | Giữ; quyết định theo lý do thay đổi, không theo số method. |
| Construction tách khỏi use | Ch. 11, tr. 154-157 | Giữ khi phù hợp cấu trúc project; không tự thêm DI container. |
| Expressiveness và ít class/method | Ch. 12, tr. 171-176 | Giữ để cân bằng nguy cơ chia nhỏ quá mức. |
| Shared state và ownership | Ch. 13, tr. 180-182 | Giữ; chọn cơ chế phù hợp runtime và phạm vi resource. |
| Precision, boundary tests | Ch. 17, tr. 301, 314 | Giữ; kiểm tra units, invariants và cases quanh boundary. |

## Điều chỉnh hoặc không dùng làm luật tuyệt đối

| Lời khuyên | Cách áp dụng có chọn lọc |
| --- | --- |
| Hàm phải rất ngắn; block thường chỉ một dòng | Không dùng ngưỡng cứng; đánh giá tổng công sức đọc và navigation. |
| Hàm lý tưởng không có tham số | Không giấu input trong mutable state; giữ explicit dependencies. |
| Tránh flag argument | Cải thiện lời gọi khó hiểu bằng named options hoặc thao tác riêng khi cần; không cấm boolean cấu hình. |
| Ưu tiên exceptions | Cho phép Result, Option và error returns theo ecosystem; không áp kiểu Java cho mọi ngôn ngữ. |
| Không trả hoặc truyền null | Dùng absence có hợp đồng; không thay semantics của API hiện có để bỏ null checks. |
| Hạn chế switch | Giữ switch/pattern matching đơn giản; chỉ thêm polymorphism nếu có lợi ích cụ thể. |
| Một assert mỗi test | Ngay trong sách, tr. 130-132 đã chấp nhận nhiều assert; ưu tiên một hành vi mỗi test. |
| Ba luật TDD | Chọn test-first theo nhiệm vụ và rủi ro; không yêu cầu TDD cho format/rename nhỏ. |
| Không lặp lại | Hợp nhất cùng policy; cho phép code giống nhau khi các policy độc lập. |
| Bọc third-party API | Chỉ bọc khi che coupling có ý nghĩa, xử lý policy chung hoặc phục vụ kiểm chứng. |
| Ưu tiên extension thay modification | Không tạo extension point giả định; sửa trực tiếp implementation đơn giản nếu đó là cách rõ nhất. |

## Phân biệt bổ sung với nội dung sách

Các guardrails về phạm vi diff, tương thích serialization/public API, báo cáo kiểm chứng trung thực, cancellation, retry/idempotency, bảo mật log và async lifecycle là **bổ sung thực hành cho coding agent**, không phải trích dẫn nguyên văn từ sách. Dùng chúng để tránh biến một cleanup thành thay đổi hành vi hoặc rủi ro vận hành.

Các ví dụ code và domain trong bộ skill được viết mới. Không kèm PDF gốc, bản trích toàn văn hoặc code samples của sách.
