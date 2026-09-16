# Kịch bản demo 10–15 phút

1. Mở catalog không đăng nhập: tìm “Nguyễn Nhật Ánh”, lọc genre/NXB/còn sách; mở chi tiết và sách cùng thể loại.
2. Member01: đăng nhập, xem các phiếu đang mượn và quá hạn, tiền phạt, thông báo; xuất hai loại PDF. Đặt một tựa đang còn bản, mở phiếu và kiểm tra hạn nhận. Thử đặt lặp lại để thấy chặn.
3. Librarian01 trong một trình duyệt riêng: mở Quản lý mượn trả, giao sách vừa đặt. Member01 tải lại để thấy trạng thái và hạn trả.
4. Member01 xin gia hạn một phiếu chưa đến hạn, không có người khác đặt cùng sách. Thủ thư duyệt, kiểm tra hạn mới. Sách demo số 11 có người khác đặt nên không được gia hạn.
5. Thủ thư nhận trả một sách đang mượn; kiểm tra số bản sẵn sàng tăng một. Thử trả lần nữa qua API để thấy từ chối.
6. Mở khoản phạt còn nợ; ghi nhận một phần thanh toán; thành viên xem lịch sử và số tiền còn nợ.
7. Member20: không có lịch sử mượn. Tạo danh sách, lưu sách từ trang chi tiết, chọn thể loại trong hồ sơ, xem gợi ý.
8. Librarian: thêm/sửa sách, thử ISBN trùng, thử giảm bản xuống dưới số đang mượn. Sách có lịch sử được lưu trữ khi xóa.
9. Admin: quản lý thể loại/tài khoản, chỉnh chính sách. Dashboard có sách phổ biến, thể loại, lượt mượn theo tháng, tỷ lệ quá hạn và tiền thu.
10. Chạy Postman Collection: session/CSRF → login → đọc catalog → đặt sách → đổi phiên librarian → checkout/return → kiểm tra quyền Member bị từ chối trên API quản trị.

Không cần sửa ngày hệ thống để demo: dữ liệu quá hạn có sẵn tính tương đối lúc tạo DB. Muốn tái lập dữ liệu ban đầu dùng một database demo mới theo README.
