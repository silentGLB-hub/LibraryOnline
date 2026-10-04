# Đối chiếu yêu cầu Topic 8

| Yêu cầu | Điểm | Triển khai / nơi demo |
|---|---:|---|
| User and Role Management | 1.0 | Account, Profile, Users; Users, AccountService và SessionController |
| Book Catalog Management | 1.5 | ManageBooks; tất cả trường bắt buộc, CRUD, ảnh, tồn kho |
| Book Search and Browsing | 0.75 | Catalog; tìm kiếm, bộ lọc, chi tiết và sách cùng thể loại |
| Reservation and Borrowing | 2.0 | Loans; reserve, checkout, return, cancel, limit và transaction |
| Overdue and Fine Management | 1.5 | Maintenance, Fines; phạt theo ngày, thanh toán, số dư |
| Loan Renewal | 0.75 | Loans/Renewals; yêu cầu trước hạn, kiểm tra chặn, duyệt/từ chối |
| Administration | 0.5 | Users, Settings, ManageBooks, Loans, Fines |
| Dashboard and Statistics | 0.75 | Dashboard; lượt mượn, sách/genre, tỷ lệ quá hạn, tiền thu, thành viên |
| Notifications and Reminders | 0.5 | Notifications; in-app, maintenance mỗi phút và khi truy cập |
| Reading Lists and Recommendations | 0.5 | Reading, Profile; danh sách cá nhân và gợi ý thể loại/lịch sử |
| PDF Reports | 0.25 | Nút Xuất PDF tại Loans/Fines; quyền sở hữu được kiểm tra phía server |
| Demo data | Bao gồm | DemoInitializer và scripts/Verify-Demo.sql |

Tổng phạm vi đối chiếu: 10.0 điểm theo đề bài. Điểm thực tế do giảng viên đánh giá; tài liệu này chỉ chỉ ra nơi đã triển khai.

## Use case theo vai trò

Member: đăng ký → đăng nhập → tìm/đặt sách → nhận tại quầy → xem hạn → xin gia hạn → trả tại quầy → xem/thanh toán phạt tại quầy → đọc báo cáo; tạo danh sách và nhận gợi ý.

Librarian: cập nhật kho sách → xử lý đặt/giao/nhận trả → duyệt gia hạn → ghi nhận tiền phạt → khóa/mở Member → xem thống kê và xuất báo cáo toàn thư viện.

Administrator: toàn bộ nghiệp vụ staff, tạo/phân quyền tài khoản, quản lý thể loại, cấu hình hạn mức/thời hạn/mức phạt và tên thư viện.
