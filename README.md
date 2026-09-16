# LibraryOnline — Thư viện Mở

Đồ án Topic 8: Online Library and Book Borrowing System. Website tiếng Việt, ASP.NET MVC 5 + Web API 2 + Entity Framework 6, SQL Server, Visual Studio Community 2026, Postman.

## Chạy nhanh

1. Mở `LibraryOnline.sln` bằng **Visual Studio Community 2026**. Cần workload **ASP.NET and web development**, .NET Framework 4.8 targeting pack, IIS Express.
2. Trong `LibraryOnline/Web.config`, kiểm tra connection string `LibraryDb`. Mặc định `Data Source=.;Initial Catalog=LibraryOnlineDemo;Integrated Security=True;...`. Nếu dùng SQL Express: đổi `Data Source=.\SQLEXPRESS`. Tài khoản Windows chạy website cần quyền tạo database này ở lần chạy đầu.
3. Chạy `powershell -ExecutionPolicy Bypass -File .\scripts\Configure-Demo.ps1` để chọn mật khẩu demo. Tệp `LibraryOnline/Web.Local.config` được tạo cục bộ và bị loại khỏi Git. Sau đó restore NuGet, chọn `LibraryOnline` làm Startup Project, nhấn **F5** hoặc **Ctrl+F5**, mở `http://localhost:5088`.
4. Lần truy cập đầu EF6 tạo database riêng `LibraryOnlineDemo` và seed dữ liệu. Không xóa hay thay thế database khác. Lần chạy đầu có thể mất khoảng một phút.
5. Cách khác: chạy `powershell -ExecutionPolicy Bypass -File .\scripts\Start-Library.ps1` ở thư mục repository.

### Tài khoản demo

Mật khẩu chung là mật khẩu bạn chọn bằng `scripts/Configure-Demo.ps1`. Không có mật khẩu được lưu trong repository. Đổi cấu hình này sau khi database đã tạo không tự đổi mật khẩu các tài khoản có sẵn; dùng chức năng đổi mật khẩu của ứng dụng.

| Vai trò | Email |
|---|---|
| Administrator | `admin@library.local` |
| Librarian | `librarian01@library.local`, `librarian02@library.local` |
| Member | `member01@library.local` đến `member20@library.local` |

## Chức năng

- Đăng ký, đăng nhập, đăng xuất, hồ sơ, đổi mật khẩu, chọn thể loại yêu thích; phân quyền Member/Librarian/Administrator tại máy chủ.
- CRUD sách đầy đủ, ảnh bìa SVG cục bộ/URL HTTPS, ISBN duy nhất, tìm theo tiêu đề/tác giả/ISBN, lọc thể loại, NXB, tác giả, tình trạng; chi tiết và sách cùng thể loại.
- Đặt sách, hủy, thủ thư giao sách và nhận trả; giữ bản ngay khi đặt; chặn hết bản, đặt trùng và vượt hạn mức. Phiếu có Reserved/Borrowed/Returned/Overdue/Cancelled.
- Quá hạn tự động, tính phạt theo ngày, thanh toán từng phần, lịch sử thanh toán; mức phạt được chụp tại lúc nhận sách.
- Xin gia hạn và duyệt/từ chối; kiểm tra lại tại thời điểm duyệt, chặn quá hạn, chạm giới hạn và có người khác đặt cùng tựa sách.
- Dashboard số lượng, lượt mượn theo tháng, sách/thể loại phổ biến, tỷ lệ quá hạn, hoạt động thành viên, tiền phạt và thu tiền.
- Thông báo trong ứng dụng: đặt thành công, sắp đến hạn, quá hạn, nhắc tiền phạt, gia hạn và thanh toán; có đánh dấu đã đọc.
- Nhiều danh sách đọc cá nhân; gợi ý theo lịch sử mượn/thể loại đã chọn, ưu tiên sách còn bản và loại sách đã đọc.
- Xuất PDF lịch sử mượn hoặc tiền phạt: thành viên chỉ thấy dữ liệu của mình, thủ thư/quản trị thấy toàn thư viện.
- Quản trị tài khoản, thể loại, chính sách; thủ thư quản lý trạng thái thành viên. Khóa thay cho xóa tài khoản để bảo toàn lịch sử.

## Đọc sách trực tuyến

- Chi tiết sách → **Xem trước nội dung**: đọc thử không cần đăng nhập.
- **Sách đang mượn → Đọc sách**: đọc toàn bộ khi đã nhận sách, còn hạn mượn.
- Có mục lục/chuyển chương, đổi cỡ chữ và nền sáng/giấy ngà/tối.
- **Quản lý sách → Nội dung & giới thiệu**: sửa tiểu sử tác giả, giới thiệu tác phẩm/NXB, đoạn đọc thử và toàn văn; xuất bản hoặc chuyển về bản nháp.

## Dữ liệu demo

Seed gồm **1 Administrator, 2 Librarian, 20 Member, 8 thể loại, 50 sách, 41 phiếu, 10 phiếu quá hạn có phạt, 5 thanh toán, 5 yêu cầu gia hạn**. Có 9 phiếu đang đặt và 2 phiếu hủy; các phiếu mượn/trả cũng bắt đầu từ yêu cầu đặt. Có thành viên nhiều phiếu và thành viên chưa có lịch sử; có sách hết bản và sách còn bản. Số liệu quá hạn/thông báo thay đổi theo thời gian thật.

Tên sách/tác giả dùng để minh họa; ISBN, thông tin ấn bản, ảnh bìa và NXB trong seed không phải dữ liệu thư mục chính thức. Ảnh bìa được tạo bằng SVG, không cần dịch vụ ảnh bên ngoài.

Chạy `scripts/Verify-Demo.sql` trong SSMS để kiểm tra số lượng và tính nhất quán tồn kho/thanh toán.

## Postman và kiểm thử

- Import `postman/LibraryOnline.postman_collection.json` và `postman/Local.postman_environment.json`.
- Nhập mật khẩu demo cục bộ vào biến `password` của environment (để trống trong Git), rồi chọn environment **LibraryOnline Local**, website phải chạy ở port 5088. Chạy collection theo thứ tự; cookie jar phải bật.
- Collection lấy CSRF token trước đăng nhập, đăng nhập rồi lấy token mới. Các request ghi dùng header `X-CSRF-Token`; không tắt bảo vệ CSRF để tiện demo.
- Bộ kiểm thử mở rộng: đặt biến môi trường `LIBRARY_DEMO_PASSWORD` bằng mật khẩu demo cục bộ, rồi chạy `python tests/integration.py --base http://localhost:5088`. Chi tiết và kết quả thực tế: `docs/TEST-RESULTS.md`.

## Cấu trúc

```
LibraryOnline.sln
LibraryOnline/
  Models/        # EF6 entities, Identity, request validation, seed
  Services/      # Transactional circulation, password hashing, maintenance
  Controllers/   # MVC pages, Web API endpoints, PDF reports
  Views/         # Razor shell
  Content/       # Responsive CSS and 50 local book covers
  Scripts/       # Client UI, calls to server APIs
postman/         # Collection + local environment
scripts/         # Start website, read-only SQL verification
tests/           # Integration and concurrency checks
docs/            # Requirements mapping, architecture, API, demo, tests
```

## Quy tắc vận hành

- Ngày đến hạn tính theo **UTC**, được phép trả đến hết ngày ghi trên phiếu. Bắt đầu phạt từ ngày kế tiếp. Gia hạn phải gửi **trước ngày đến hạn**.
- Hạn mức gồm Reserved + Borrowed + Overdue. Mỗi thành viên chỉ có một phiếu đang hoạt động trên cùng tựa sách.
- Giao dịch ghi dùng SQL transaction Serializable + `sp_getapplock`; book rowversion chặn ghi đè chỉnh sửa cũ. Không cho tổng bản thấp hơn số bản đang giữ.
- Sách có lịch sử được lưu trữ khi xóa; tài khoản được khóa. Những bản ghi chưa được tham chiếu có thể xóa thật.
- Maintenance chạy mỗi phút khi IIS đang hoạt động và trước mỗi API request. Khi IIS ngủ/dừng, không có job nền chạy; dữ liệu sẽ được tính bù khi website nhận request tiếp theo. Triển khai cần nhắc đúng thời điểm khi website ngủ thì cấu hình IIS Always Running hoặc gọi endpoint đọc định kỳ bằng scheduler.
- Thông báo là **in-app**, không có tích hợp email/SMS; không có thanh toán trực tuyến. Ghi nhận thu tiền do thủ thư thực hiện sau khi nhận tiền tại quầy.
- Có trang đọc theo chương, đoạn xem trước công khai và thông tin tác giả/tác phẩm/NXB. Thủ thư nhập văn bản được phép phân phối tại **Quản lý sách → Nội dung & giới thiệu**. Nội dung có sẵn là văn bản minh họa tự viết, không phải nguyên tác của các tựa sách demo. Xem `docs/READING.md`.
- Seed dùng `CreateDatabaseIfNotExists`, không tự xóa/tái tạo schema. Khi sửa model sau này cần migration có kiểm soát. Muốn demo lại từ đầu, đổi sang tên database demo mới và chạy lại (không tự động xóa database hiện tại).

## Bảo mật và triển khai

ASP.NET Identity quản lý người dùng/vai trò; mật khẩu dùng PBKDF2-SHA256 150.000 vòng với salt ngẫu nhiên. Forms Authentication dùng cookie HttpOnly, SameSite và kiểm tra SecurityStamp mỗi request; đổi mật khẩu/khóa tài khoản vô hiệu hóa phiên cũ. Khóa đăng nhập tạm sau 5 lần sai. Các API ghi kiểm tra CSRF và quyền sở hữu; dữ liệu trả về không chứa password hash.

Bản này cấu hình cho demo localhost. Trước triển khai công khai: dùng HTTPS và cookie `requireSSL`, tắt debug, tắt `DemoSeedEnabled` trước khởi tạo DB, tạo tài khoản quản trị theo quy trình riêng, thay tài khoản demo, cấp quyền SQL tối thiểu, cấu hình sao lưu/giám sát và machineKey an toàn nếu chạy nhiều instance. Không commit mật khẩu SQL, access token hoặc database backup lên GitHub.

Tham khảo: [MVC 5 với EF6 — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/mvc/overview/getting-started/getting-started-with-ef-using-mvc/creating-an-entity-framework-data-model-for-an-asp-net-mvc-application).
