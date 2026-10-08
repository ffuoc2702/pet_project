# Test và bằng chứng kiểm chứng

## Chọn test theo rủi ro

- Với bug fix, viết regression test tái hiện lỗi tại mức nhỏ nhất có ý nghĩa. Nếu có thể, xác nhận test thất bại trước bản sửa và pass sau bản sửa.
- Với tính năng mới hoặc quy tắc nghiệp vụ, test outcome được yêu cầu và boundary/failure cases liên quan.
- Với refactor có rủi ro, chạy tests hiện có; thêm characterization test cho hành vi thiếu bảo vệ trước khi đổi cấu trúc. Không đóng băng hành vi lỗi đã được yêu cầu sửa.
- Với rename hoặc format nhỏ, dùng build/typecheck/linter và tests phù hợp; không viết test chỉ để chứng minh tên hay số dòng thay đổi.

Không áp TDD cho mọi thao tác. Dùng test-first khi giúp làm rõ requirement, sửa lỗi hoặc bảo vệ refactor; dùng chu trình phù hợp với công việc còn lại.

## Test rõ và ổn định

Dùng tên diễn đạt điều kiện và kết quả. Giữ arrange-act-assert hoặc given-when-then theo convention. Một test tập trung một hành vi; cho phép nhiều assert mô tả cùng outcome, ví dụ status và body của một response.

Giữ test độc lập, không phụ thuộc thứ tự, thời gian hiện tại hoặc dữ liệu dùng chung không kiểm soát. Với logic thời gian/random, dùng clock/seed hoặc seam phù hợp. Dùng synchronization thay sleep để đợi điều kiện khi kiểm thử concurrency.

Tách unit tests nhanh khỏi integration tests cần infrastructure. Cô lập state và cố định fixtures cho integration tests; không yêu cầu mọi integration test chạy offline.

Dùng mock/fake ở ranh giới khó kiểm soát. Không mock mọi class, private helper hoặc chi tiết triển khai. Assert collaborator interaction khi interaction chính là contract, như gửi đúng một thông báo; dùng test phù hợp để kiểm chứng boundary thực khi cần.

## Chọn dữ liệu kiểm thử

Ưu tiên empty/missing, một phần tử, giá trị ngay dưới/trên boundary, duplicate, invalid state, failure và các nhánh vừa thay đổi. Chỉ dùng cases liên quan domain; không nhân ma trận input vô hạn.

Ví dụ tự viết: quy tắc đặt sân tối đa 120 phút nên được kiểm tra tại 119, 120 và 121 phút, cùng đầu vào không hợp lệ nếu contract có quy định. Không suy ra giá trị 120 nếu requirement chưa nêu.

Dùng coverage để tìm vùng chưa được thực thi; không xem 100% coverage là bằng chứng đúng đắn. Giữ assertions có ý nghĩa; không snapshot output biến động hoặc chỉ test cấu trúc implementation.

## Chạy và báo cáo

Dùng công cụ hiện có của repository. Chạy tests trực tiếp liên quan trước, sau đó các checks bắt buộc và phạm vi rộng hơn nếu coupling/rủi ro yêu cầu. Không lặp suite đã pass nếu không có thay đổi hay nghi vấn mới.

Ghi rõ lệnh/check đã chạy và kết quả. Nếu không chạy được do dependency hoặc environment, nói rõ phần chưa kiểm chứng. Không sửa test ngoài phạm vi hoặc che lỗi để đạt màu xanh.
