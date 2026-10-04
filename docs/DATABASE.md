# Cơ sở dữ liệu đơn giản

Ứng dụng dùng SQL Server, database `LibraryOnlineDemo`. Bản ngày 04/10/2026 gộp 5 bảng tài khoản Identity thành **Users**. Mỗi người dùng chỉ có một vai trò trong cột **Role**: Member (độc giả), Librarian (thủ thư), Administrator (quản trị). Không dùng bảng đăng nhập ngoài, claims hoặc bảng nối vai trò.

## Các bảng cần hiểu

| Bảng | Lưu gì? | Liên kết chính |
|---|---|---|
| Users | Tài khoản, họ tên, email, vai trò | Id được dùng làm MemberId |
| Categories | Thể loại | Books.CategoryId |
| Books | Tên sách, tác giả, NXB, số lượng | Mỗi sách thuộc một thể loại |
| Loans | Đặt sách, mượn, trả, hạn trả | MemberId → Users; BookId → Books |
| Fines | Tiền phạt, số đã thanh toán | LoanId → Loans |
| FinePayments | Từng lần đóng tiền phạt | FineId → Fines |
| RenewalRequests | Yêu cầu gia hạn | LoanId → Loans |
| LibraryPolicies | Quy định chung, chỉ một dòng | Số sách tối đa, số ngày mượn, mức phạt |
| BookMaterials | Nội dung đọc, xem trước, giới thiệu | BookId → Books, mỗi sách một dòng |
| Notifications | Thông báo cho độc giả | MemberId là Id độc giả |
| ReadingLists | Danh sách đọc cá nhân | MemberId là Id độc giả |
| ReadingListItems | Sách trong danh sách | ReadingListId → ReadingLists; BookId → Books |
| GenrePreferences | Thể loại yêu thích | MemberId là Id độc giả; CategoryId → Categories |

Có **13 bảng nghiệp vụ**. `LibraryFeatureVersions` là bảng kỹ thuật nhỏ để ghi nhận dữ liệu đọc mẫu đã được tạo, tránh ghi đè khi khởi động lại. `sysdiagrams` do SSMS tạo khi lưu sơ đồ. Database mới do EF tạo có thể có `__MigrationHistory`, cũng là metadata kỹ thuật.

Không tách tác giả và NXB thành nhiều bảng: tên nằm ngay trong Books; giới thiệu nằm trong BookMaterials. Một phiếu đặt sách chuyển thành phiếu mượn nên không cần thêm bảng Reservations. Giữ riêng các lần thanh toán và gia hạn để xem được lịch sử, đúng yêu cầu đề bài.

## Ví dụ dễ nhớ

Một độc giả trong Users chọn một sách trong Books → tạo một dòng Loans. Khi nhận sách, Status đổi từ 0 sang 1. Trả sách thì đổi sang 2. Nếu trả trễ, tiền phạt nằm trong Fines; mỗi lần trả tiền thêm một dòng FinePayments.

Trạng thái Loans: 0 = Reserved, 1 = Borrowed, 2 = Returned, 3 = Overdue, 4 = Cancelled.
Trạng thái RenewalRequests: 0 = Pending, 1 = Approved, 2 = Rejected.

## Các file SQL

- `LibraryOnline/App_Data/LibraryOnline.Schema.sql`: tạo các bảng nghiệp vụ trong database trống; có chú thích trước mỗi bảng. Không chứa tài khoản hay mật khẩu.
- `LibraryOnline/App_Data/SimplifyDatabase.sql`: chuyển database cũ sang Users, giữ nguyên ID, mật khẩu đã băm và phiếu mượn. **Sao lưu trước khi chạy**, chọn đúng database. Script dừng nếu có đăng nhập ngoài hoặc tài khoản có nhiều vai trò.
- `LibraryOnline/App_Data/Verify-Demo.sql`: kiểm tra số lượng demo và tồn kho.

Để tạo demo mới, để ứng dụng tự tạo database chưa tồn tại theo README; chạy schema thủ công không gọi trình tạo dữ liệu mẫu.

Mật khẩu vẫn được băm PBKDF2. SecurityStamp vô hiệu phiên đăng nhập cũ khi đổi mật khẩu/khóa tài khoản. AccessFailedCount và LockoutEndDateUtc ngăn thử mật khẩu liên tục. Không bỏ các trường này để đơn giản hóa hình thức.
