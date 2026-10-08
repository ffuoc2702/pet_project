---
name: pragmatic-clean-code
description: Áp dụng có chọn lọc các nguyên tắc Clean Code khi viết, sửa lỗi, refactor hoặc review code. Dùng khi cần cải thiện tên, trách nhiệm, cấu trúc, xử lý lỗi, test và ranh giới phụ thuộc mà vẫn giữ đúng hợp đồng và tránh overengineering. Phù hợp nhiều ngôn ngữ; ưu tiên convention của repository.
---

# Pragmatic Clean Code

Viết code để người tiếp theo hiểu được ý định, thay đổi an toàn và kiểm chứng được hành vi. Dùng các nguyên tắc như công cụ ra quyết định, không dùng như bộ chỉ tiêu hình thức.

## Thứ tự ưu tiên

1. Tuân thủ chỉ dẫn cấp cao hơn, yêu cầu người dùng và hướng dẫn repository.
2. Bảo đảm tính đúng đắn, bảo mật, hợp đồng và ràng buộc vận hành.as
3. Giảm chi phí đọc, kiểm chứng và thay đổi code.
4. Giữ convention; tối ưu hình thức khi có lợi ích cụ thể.

Dùng **MUST** cho điều kiện bắt buộc trong phạm vi công việc; **SHOULD** cho mặc định có thể điều chỉnh bằng lý do kỹ thuật; **AVOID** cho cách làm thường tăng chi phí. Không diễn giải mọi SHOULD thành MUST.

## Quy trình

1. **Đọc ngữ cảnh.** Kiểm tra hướng dẫn repository, code gần vị trí sửa, callers, tests và cấu hình formatter/linter. Xác định hợp đồng: đầu vào, đầu ra, lỗi, side effects, thứ tự và hiệu năng liên quan.
2. **Khoanh vùng.** Nêu vấn đề cụ thể và tiêu chí thành công. Với review, dẫn vị trí và hậu quả; tách lỗi đúng đắn khỏi đề xuất phong cách. Không sửa cả repository chỉ vì một code smell.
3. **Chọn thay đổi nhỏ nhất hữu ích.** Thử đổi tên, làm rõ điều kiện hoặc bỏ phần thừa trước khi thêm hàm, interface hay layer. Chỉ tạo abstraction khi nó giảm một chi phí đã quan sát được.
4. **Triển khai.** Với refactor, giữ hành vi quan sát được. Với bug fix hoặc tính năng mới, thực hiện thay đổi hợp đồng được yêu cầu và giữ các hành vi còn lại. Không trộn nâng cấp dependency hoặc sửa API vào cleanup.
5. **Kiểm chứng theo rủi ro.** Chạy kiểm tra hiện có phù hợp. Thêm test cho hành vi mới, lỗi đã sửa hoặc vùng thiếu bảo vệ có rủi ro thực tế. Không tạo test chỉ lặp lại implementation.
6. **Đọc lại diff và dừng.** Kiểm tra callers, lỗi, tài liệu, test và chi phí mới. Dừng khi mục tiêu đã đạt; không tiếp tục chia nhỏ vì còn có thể chia.
7. **Báo cáo.** Nêu thay đổi, lý do, kiểm tra thực tế đã chạy và giới hạn còn lại. Không tuyên bố test pass khi chưa chạy.

## Quy tắc cốt lõi

### MUST

- Giữ hợp đồng khi refactor, bao gồm null/empty, loại lỗi, thứ tự, side effects và tương thích callers. Không đổi public symbol mà bỏ qua API, serialization hoặc consumer bên ngoài.
- Không biến thất bại thành kết quả thành công giả: không nuốt exception hoặc trả mặc định che lỗi. Giữ nguyên cause/stack khi truyền hoặc chuyển đổi lỗi.
- Làm rõ những thay đổi trạng thái đáng kể trong tên, hợp đồng hoặc tài liệu; không che mutation trong hàm trông như truy vấn thuần.
- Tuân thủ cơ chế bảo vệ invariant, authorization và ownership hiện có. Không bỏ validation cần thiết để code ngắn hơn.
- Không làm suy yếu test, xóa assertion hoặc đổi expected value chỉ để làm bản sửa pass. Không sửa phần việc không liên quan của người dùng.

### SHOULD

- Đặt tên theo ý định và thuật ngữ domain; nêu đơn vị khi dễ nhầm. Giữ một từ nhất quán cho cùng khái niệm.
- Giữ hàm tập trung vào một mục đích ở mức trừu tượng hợp lý. Cho phép hàm điều phối gồm nhiều bước của cùng use case.
- Gom trách nhiệm thường thay đổi cùng nhau; bảo vệ invariant gần nơi sở hữu dữ liệu. Ưu tiên coupling thấp và cohesion cao hơn số dòng ít.
- Tách quy tắc nghiệp vụ khỏi chi tiết I/O khó kiểm chứng khi có nhu cầu cụ thể. Tận dụng ranh giới sẵn có.
- Loại bỏ sự lặp lại của **cùng một quy tắc**, không hợp nhất chỉ vì code có hình thức giống nhau.
- Giữ comment giải thích lý do, invariant, giới hạn hoặc quyết định khó suy ra. Sửa comment lỗi thời cùng với code.
- Viết test dễ đọc, độc lập, lặp lại được và tự xác định pass/fail; tập trung hành vi và boundary conditions.
- Dùng formatter/linter có sẵn; giữ diff tập trung.

### AVOID

- Áp ngưỡng cứng cho số dòng, số tham số, số assert hoặc độ sâu nesting nếu repository không yêu cầu.
- Chuyển tham số thành mutable fields chỉ để giảm số tham số.
- Tạo interface cho mọi class, wrapper cho mọi thư viện hoặc generic framework cho một trường hợp.
- Chia code thành chuỗi hàm một dòng khiến người đọc phải nhảy qua nhiều nơi để hiểu một thao tác đơn giản.
- Cấm toàn bộ switch, null, boolean arguments, exceptions hoặc comments theo giáo điều.
- Tối ưu hoặc thay collection/query mà không kiểm tra lazy evaluation, materialization, thứ tự và chi phí I/O.

## Đọc tham chiếu theo nhu cầu

Chỉ đọc tài liệu liên quan đến phần việc hiện tại:

| Vấn đề | Tài liệu |
| --- | --- |
| Tên, tham số, điều kiện, tách hàm, comments | [naming-and-functions.md](references/naming-and-functions.md) |
| SRP, duplication, interface, DTO, I/O | [design-and-boundaries.md](references/design-and-boundaries.md) |
| Exception, Result, null, cleanup | [error-handling.md](references/error-handling.md) |
| Bug fix, refactor thiếu test, assertions, mocks | [testing.md](references/testing.md) |
| Shared state, async, synchronization | [concurrency.md](references/concurrency.md) |
| Lý do chọn hoặc điều chỉnh lời khuyên từ sách | [principle-selection.md](references/principle-selection.md) |

## Kiểm tra trước khi hoàn tất

- Người đọc có hiểu mục đích qua tên và luồng chính không?
- Thay đổi có giải quyết vấn đề cụ thể trong phạm vi được yêu cầu không?
- Có hợp đồng, trạng thái hoặc failure mode nào bị đổi ngoài ý muốn không?
- Abstraction mới có lợi ích thực tế lớn hơn chi phí đi qua nó không?
- Bằng chứng kiểm chứng có tương xứng với rủi ro không?
