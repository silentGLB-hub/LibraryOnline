# Kết quả kiểm thử

Kiểm thử thực tế ngày 16/09/2026 trên Windows, Visual Studio Community 2026 (MSBuild 18.10), .NET Framework 4.8, IIS Express localhost:5088 và SQL Server cục bộ.

## Kết quả

- Build Debug và Release: thành công, không cảnh báo ở build cuối.
- Python integration: **63/63 kiểm tra đạt**.
- Postman Collection chạy bằng Newman: **26 request, 29 assertions, 0 lỗi**.
- SQL kiểm tra seed ban đầu: 1 Admin, 2 Librarian, 20 Member, 8 thể loại, 50 sách, 41 phiếu, 10 quá hạn, 10 khoản phạt, 5 thanh toán, 5 gia hạn. Không có sai lệch tồn kho hoặc tổng thanh toán.
- Kiểm tra trình duyệt: catalog, tiếng Việt, đăng nhập Member, trang mượn, menu theo vai trò, kích thước điện thoại 390px không tràn ngang; không có lỗi JavaScript trong console ở các trang đã kiểm tra.
- PDF: header Unicode đúng, mở/render thành công, không có chữ bị cắt/chồng trong mẫu kiểm tra.

## Kiểm tra tự động

- ✓ Unauthenticated loans rejected
- ✓ Missing CSRF rejected
- ✓ Administrator sign in
- ✓ Librarian sign in
- ✓ Eight seeded genres
- ✓ Fifty seeded books
- ✓ Stock never negative
- ✓ Dashboard works
- ✓ Search q=Clean
- ✓ Search author=Martin
- ✓ Search publisher=NXB
- ✓ Search genre=4
- ✓ Search availability=unavailable
- ✓ MVC page Catalog
- ✓ MVC page Dashboard
- ✓ MVC page ManageBooks
- ✓ MVC page Users
- ✓ MVC page Settings
- ✓ MVC page Loans
- ✓ MVC page Fines
- ✓ MVC page Profile
- ✓ MVC page Notifications
- ✓ Register creates Member only
- ✓ Member cannot access admin data
- ✓ Librarian cannot change policy
- ✓ Create catalog record
- ✓ Stale book rowversion rejected
- ✓ Duplicate ISBN rejected
- ✓ Concurrent last-copy reservation: exactly one succeeds
- ✓ Last-copy stock is zero
- ✓ Cannot alter another member loan
- ✓ Member cannot checkout own loan
- ✓ Librarian checkout
- ✓ One pending renewal per loan
- ✓ Approved renewal extends due date
- ✓ Overdue auto calculation
- ✓ Overdue status set
- ✓ Overdue renewal blocked
- ✓ Overpayment rejected
- ✓ Member cannot record payment
- ✓ Partial payment and ledger
- ✓ Double return rejected
- ✓ Return releases one copy
- ✓ Other reservation blocks renewal
- ✓ Reading list ownership enforced
- ✓ Recommendation API works
- ✓ Reservation/overdue/payment notifications
- ✓ Unicode PDF export loans
- ✓ Unicode PDF export fines
- ✓ Member data isolation
- ✓ Maximum active loan limit enforced
- ✓ Cannot reduce stock below held copies
- ✓ Expired reservation automatically cancelled
- ✓ Expiry releases stock
- ✓ Approval rechecks newly created reservations
- ✓ Maximum renewal count enforced
- ✓ Password change logs out current session
- ✓ Login with new password succeeds
- ✓ Responses exclude password hashes and security stamps
- ✓ Disabled account invalidates existing session
- ✓ Disabled account invalidates existing session
- ✓ Archive preserves loan history and hides book
- ✓ SQL stock invariant after all mutations

## Tái chạy

Website cần đang chạy. Đặt biến môi trường `LIBRARY_DEMO_PASSWORD` bằng mật khẩu demo cục bộ trước khi chạy Python; điền biến `password` trong bản sao environment cục bộ trước khi chạy Newman. Không commit bản sao chứa mật khẩu. Dùng Python 3 với thư viện chuẩn và SQLCMD có sẵn trong PATH:

```
python tests/integration.py --base http://localhost:5088 --output TestResults/integration.json
npx --yes --package=newman newman run postman/LibraryOnline.postman_collection.json -e postman/Local.postman_environment.json
```

Integration tạo tài khoản/sách QA riêng, thay ngày đến hạn của phiếu QA để kiểm thử quá hạn, rồi khóa tài khoản/lưu trữ sách QA khi hoàn tất. Không sửa phiếu demo có sẵn. Nếu bài test thất bại giữa chừng, dữ liệu QA có thể còn hoạt động. Chỉ chạy trên database demo cục bộ. Tệp TestResults chứa cookie/session kiểm thử nên bị loại khỏi Git.

Các kiểm tra này không thay thế kiểm thử tải, penetration test hoặc kiểm thử triển khai production.
