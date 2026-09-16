# API

Base URL: `http://localhost:5088/api`. JSON dùng PascalCase cho entity/DTO, một số response tổng hợp dùng camelCase; Postman và app.js chứa ví dụ chạy được.

GET `/session` trả csrfToken và user; cookie anti-forgery được đặt tự động. POST login với `{Email,Password}`, rồi GET `/session` lại để lấy token tương ứng identity mới. Mọi POST/PUT/DELETE cần `X-CSRF-Token` và cookie jar.

| Endpoint | Quyền | Chức năng |
|---|---|---|
| GET session; POST session/login, session/register | Công khai + CSRF cho POST | Phiên, đăng nhập, đăng ký Member |
| POST session/logout, session/password; PUT session/profile | Đã đăng nhập | Tài khoản hiện tại |
| GET books; GET books/{id}; GET categories | Công khai | Catalog, chi tiết, thể loại |
| POST books; PUT/DELETE books/{id} | Librarian, Administrator | Quản lý sách |
| POST categories; PUT/DELETE categories/{id} | Administrator | Thể loại |
| GET loans, renewals, fines | Đã đăng nhập | Member chỉ xem dữ liệu của mình |
| POST loans `{BookId}` | Member | Đặt sách |
| POST loans/{id}/actions `{Action}` | Chủ phiếu hoặc staff | cancel; staff thêm checkout/return |
| POST loans/{id}/renewals | Member, chủ phiếu | Xin gia hạn |
| POST renewals/{id}/decision `{Approve,Note}` | Staff | Duyệt/từ chối |
| POST fines/{id}/payments `{Amount,Note}` | Staff | Ghi nhận thanh toán |
| GET notifications; POST notifications/{id}/read | Chủ thông báo | Thông báo cá nhân |
| GET/POST reading-lists; DELETE reading-lists/{id} | Member, chủ danh sách | Danh sách đọc |
| POST reading-lists/{id}/books `{BookId}`; DELETE reading-lists/{id}/books/{bookId} | Chủ danh sách | Lưu/bỏ sách |
| GET recommendations | Member | Gợi ý cá nhân |
| GET users; PUT users/{id} `{Role,IsActive}` | Staff | Librarian chỉ khóa/mở Member |
| POST users | Administrator | Tạo tài khoản và vai trò |
| GET policy; PUT policy | Staff đọc, Administrator ghi | Chính sách |
| GET dashboard | Staff | Thống kê |
| GET /Reports/Export?kind=loans hoặc fines | Đã đăng nhập | PDF ngoài prefix /api |

Lỗi thường gặp: 400 validation, 401 chưa đăng nhập, 403 sai quyền/CSRF, 404 không có dữ liệu, 409 vi phạm nghiệp vụ/concurrency. API không chuyển 401 sang trang HTML login.

Query catalog: `q`, `genre`, `author`, `publisher`, `availability=available|unavailable`, `page`, `pageSize` (1–100). Staff thêm `manage=true` để thấy sách lưu trữ. Khi sửa sách, gửi `Version` lấy từ `RowVersion` trong GET chi tiết để chống ghi đè dữ liệu cũ.
