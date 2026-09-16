# Đọc sách, xem trước và thông tin xuất bản

## Dành cho độc giả

1. Mở chi tiết sách trong catalog. Phần **Đọc & khám phá tác phẩm** có đoạn xem trước và các mục tác giả, tác phẩm, nhà xuất bản.
2. **Xem trước nội dung** mở `/Reader?book=ID&preview=1`. Không cần tài khoản. Máy chủ chỉ trả PreviewText, không gửi FullText xuống trình duyệt.
3. Sau khi thủ thư giao sách (Borrowed), chọn **Sách đang mượn → Đọc sách**, hoặc **Đọc sách ngay** ở chi tiết sách.
4. Dùng mục lục hoặc nút Chương trước/tiếp; đổi cỡ chữ và nền đọc.

Quyền đọc toàn văn cần phiếu thuộc chính người dùng, trạng thái Borrowed, chưa ReturnedAt và DueAt chưa qua ngày UTC hiện tại. Reserved, Overdue, Returned, Cancelled không cấp quyền đọc. Thủ thư/quản trị có quyền xem để kiểm tra nội dung.

Máy chủ kiểm tra quyền mỗi lần gọi API/chuyển chương. Trang kiểm tra lại mỗi 60 giây và xóa nội dung hiển thị nếu quyền không còn. Response đặt no-store. Đây là kiểm soát truy cập, không phải DRM và không ngăn người đọc lưu nội dung đã được cấp hợp lệ.

## Dành cho thủ thư

Vào **Quản lý sách → Nội dung & giới thiệu**:

- Thông tin tác giả, giới thiệu tác phẩm, giới thiệu/liên hệ NXB: tối đa 6.000 ký tự mỗi trường, có thể ghi nguồn tham khảo dạng văn bản.
- Đoạn xem trước: tối đa 6.000 ký tự, độc lập với toàn văn.
- Toàn văn: văn bản thuần tối đa 500.000 ký tự. Mỗi chương bắt đầu bằng tiêu đề; ngăn cách các chương bằng một dòng chỉ chứa `---`.
- Bản nháp: tắt đọc toàn văn và đoạn xem trước. Xuất bản cần nội dung không trống.
- Nhãn minh họa giúp phân biệt nội dung mẫu với nguyên tác. Chỉ bỏ nhãn khi đã nhập nội dung tác phẩm được phép phân phối.

Thông tin giới thiệu vẫn hiển thị khi bản đọc ở trạng thái nháp; trạng thái nháp chỉ áp dụng cho PreviewText/FullText. Mọi nội dung được hiển thị như văn bản và escape HTML để tránh thực thi mã chèn vào.

## SQL Server và dữ liệu mẫu

Bảng `dbo.BookMaterials` liên kết BookId với Books, có FK cascade khi xóa thật một sách chưa có lịch sử. Bảng `dbo.LibraryFeatureVersions` ghi lần nâng cấp. Migration cộng thêm ở `LibraryOnline/App_Data/ReadingSchema.sql`, chạy trong transaction và khóa ứng dụng khi khởi động. Không thay EF6 model hash, không reset DB, không sửa sách/tài khoản/phiếu mượn đang có.

Khi bật DemoSeedEnabled, lần nâng cấp đầu thêm nội dung mẫu 3 chương cho 50 bản ghi sách demo. Đây là bài hướng dẫn đọc tự viết, **không phải toàn văn tác phẩm thực tế**. Tiểu sử/giới thiệu chưa xác minh được để rõ là chưa có dữ liệu; thủ thư có thể nhập dữ liệu có nguồn. Khởi động lại không ghi đè nội dung thủ thư đã sửa hoặc tái xuất bản bản nháp.

Nâng cấp schema tự động yêu cầu tài khoản SQL chạy ứng dụng có quyền tạo bảng; ở môi trường hạn chế quyền nên để người quản trị áp dụng migration trước. Database cục bộ của dự án đã được nâng cấp.

## API

| Endpoint | Quyền | Dữ liệu |
|---|---|---|
| GET /api/books/{id}/information | Công khai | Tác giả/tác phẩm/NXB, trạng thái nội dung, quyền đọc của phiên hiện tại |
| GET /api/books/{id}/preview | Công khai | Chỉ PreviewText đã xuất bản |
| GET /api/books/{id}/reader?chapter=1 | Người đang mượn hợp lệ hoặc staff | Chương được yêu cầu, mục lục, ngày hết quyền |
| GET /api/books/{id}/material | Librarian/Administrator | Nội dung biên tập và Version |
| PUT /api/books/{id}/material | Librarian/Administrator + CSRF | Cập nhật nội dung; gửi Version để chống ghi đè |

## Kiểm thử

`tests/reader.py`: 21 kiểm tra thực tế đã đạt. Chạy với biến môi trường LIBRARY_DEMO_PASSWORD và website cục bộ đang mở. Bài kiểm thử dùng phiếu demo Member01 để đọc và tạo/xóa một fixture riêng để kiểm tra xuất bản, checkout, trả sách; không sửa phiếu demo cũ.

Import thêm `postman/Reader.postman_collection.json`, dùng environment Local và mật khẩu demo cục bộ. Collection đọc dữ liệu demo và kiểm tra quyền, không sửa sách/phiếu.
