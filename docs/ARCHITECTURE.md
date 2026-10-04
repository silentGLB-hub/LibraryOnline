# Kiến trúc và nghiệp vụ

Browser → MVC Razor page → JavaScript UI → Web API 2 → LibraryService → EF6 → SQL Server.

MVC HomeController phục vụ các trang và chặn trang quản trị không đúng vai trò. API kiểm tra quyền độc lập, không tin việc ẩn nút trên giao diện. DTO giới hạn trường cho phép ghi, tránh mass assignment. Chỉ service thay đổi AvailableCopies, trạng thái phiếu, tiền phạt và thanh toán.

## Mô hình dữ liệu

- Users: mỗi tài khoản có một Role, không cần bảng vai trò hay bảng nối.
- Category 1–N Book. ISBN duy nhất; RowVersion hỗ trợ optimistic concurrency.
- Member 1–N Loan; Book 1–N Loan. Reserved giữ một bản sách. Trả/hủy/hết hạn giải phóng đúng một bản.
- Loan 1–0..1 Fine; Fine 1–N FinePayment. Fine.LoanId có unique index; số đã thu phải bằng tổng giao dịch thanh toán.
- Loan 1–N RenewalRequest. Chỉ một yêu cầu Pending tại một thời điểm, được bảo vệ bằng transaction/app lock.
- Member 1–N Notification; EventKey unique giúp maintenance không tạo trùng một sự kiện.
- Member 1–N ReadingList; ReadingList 1–N ReadingListItem; cặp ListId/BookId unique.
- Member 1–N GenrePreference; cặp MemberId/CategoryId unique.
- LibraryPolicy: một bản ghi chính sách dùng chung. FinePerDay được sao chép vào Loan khi checkout.

## Luồng trạng thái

Reserved → Borrowed → Returned. Borrowed → Overdue → Returned. Reserved → Cancelled khi thành viên/thủ thư hủy hoặc hết hạn nhận.

Gia hạn: Pending → Approved hoặc Rejected. Duyệt thành công thêm RenewalDays vào DueAt, tăng RenewalCount. Trả sách tự từ chối các yêu cầu gia hạn còn Pending.

## Tính phạt

`daysLate = max(0, UTC_today - DueAt.Date)`.

`Fine.Amount = daysLate * Loan.DailyFineRate` khi đang quá hạn. Sau khi trả, khoản phạt được chốt và không tăng nữa. `Outstanding = Amount - Paid`. Không chấp nhận thanh toán âm, bằng 0, tiền lẻ VND hay vượt dư nợ.

## Đồng thời

Các thao tác mượn/trả/gia hạn/phạt và chỉnh sửa quản trị dùng cùng transaction Serializable và lock SQL theo database. Đây là lựa chọn đơn giản, dễ kiểm chứng cho quy mô đồ án, đánh đổi thông lượng ghi. Có thể tối ưu theo member/book khi mở rộng nhưng phải giữ thứ tự khóa nhất quán để tránh deadlock.

## Giới hạn đã biết

Trang quản lý sách tải tối đa 100 tựa trong bản demo; catalog có phân trang phía server. Danh sách giao dịch và báo cáo tải toàn bộ dữ liệu phù hợp với demo nhỏ; triển khai lớn cần phân trang/lọc thời gian. Chưa có xác minh email, khôi phục mật khẩu qua email, audit log đầy đủ hay tích hợp cổng thanh toán.
