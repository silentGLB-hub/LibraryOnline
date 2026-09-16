using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using LibraryOnline.Models;

namespace LibraryOnline.Services
{
    public static class BookContentStore
    {
        private static SqlParameter P(string name, object value)
        {
            return new SqlParameter(name, value ?? DBNull.Value);
        }

        public static BookMaterial Get(LibraryDb db, int id)
        {
            return db
                .Database.SqlQuery<BookMaterial>(
                    "SELECT * FROM dbo.BookMaterials WHERE BookId=@id",
                    P("@id", id)
                )
                .SingleOrDefault();
        }

        public static void Initialize()
        {
            using (var s = new LibraryService())
            {
                s.Write(() =>
                {
                    s.Db.Database.ExecuteSqlCommand(
                        File.ReadAllText(HostingEnvironment.MapPath("~/App_Data/ReadingSchema.sql"))
                    );
                    // Seed exactly once per database. Never recreate deleted/unpublished material
                    // on a later restart, and never overwrite librarian edits.
                    s.Db.Database.ExecuteSqlCommand(
                        "IF OBJECT_ID(N'dbo.LibraryFeatureVersions',N'U') IS NULL CREATE TABLE dbo.LibraryFeatureVersions (Feature nvarchar(100) NOT NULL PRIMARY KEY, AppliedAt datetime2 NOT NULL);"
                    );
                    bool seeded =
                        s.Db.Database.SqlQuery<int>(
                                "SELECT COUNT(*) FROM dbo.LibraryFeatureVersions WHERE Feature=N'BookReader-v1'"
                            )
                            .Single() > 0;
                    if (!seeded)
                    {
                        if (ConfigurationManager.AppSettings["DemoSeedEnabled"] == "true")
                        {
                            foreach (var b in s.Db.Books.Where(x => x.Id <= 50).ToList())
                            {
                                if (Get(s.Db, b.Id) != null)
                                    continue;
                                string notice =
                                    "NỘI DUNG MINH HỌA DO HỆ THỐNG TẠO — KHÔNG PHẢI NGUYÊN VĂN CỦA TÁC PHẨM.\n\n";
                                string opening =
                                    "Chương 1 · Bắt đầu một hành trình đọc\n\nMỗi lần mở một cuốn sách là một cơ hội để đặt câu hỏi mới. Trước khi đọc, hãy dành ít phút quan sát tên tác phẩm, thông tin ấn bản và phần giới thiệu. Ghi lại điều khiến bạn tò mò để so sánh với cảm nhận sau khi đọc.\n\nChọn một nơi yên tĩnh, ánh sáng vừa đủ và một khoảng thời gian phù hợp. Bạn không cần đọc thật nhanh; điều quan trọng là hiểu và suy nghĩ về những gì mình đang đọc.";
                                string full =
                                    opening
                                    + "\n---\nChương 2 · Đọc chủ động\n\nKhi gặp một ý tưởng đáng chú ý, hãy thử diễn đạt lại bằng ngôn ngữ của chính mình. Một câu hỏi tốt có thể giúp bạn hiểu sâu hơn nhiều trang ghi chép. Phân biệt thông tin trong văn bản với suy luận cá nhân, và tìm lại ngữ cảnh trước khi kết luận.\n\nBạn có thể tạo một danh sách các câu hỏi: Nội dung này nói về điều gì? Chi tiết nào hỗ trợ nhận định đó? Mình còn chưa hiểu điểm nào? Hãy dành thời gian cho những câu hỏi chưa có đáp án.\n---\nChương 3 · Sau trang cuối\n\nSau khi đọc xong, hãy viết một đoạn ngắn về điều bạn muốn nhớ. Kết nối điều vừa đọc với trải nghiệm của bạn, rồi chọn một việc nhỏ có thể thực hành. Chia sẻ cảm nhận với người khác cũng là cách tìm thấy những góc nhìn mới.\n\nTrở lại thư viện để ghi chú vào danh sách đọc, tìm một tác phẩm liên quan và kiểm tra ngày đến hạn. Hành trình đọc có thể tiếp tục từ một câu hỏi rất nhỏ.\n\nKết thúc tài liệu minh họa. Thủ thư có thể thay nội dung này bằng văn bản được phép phân phối.";
                                Insert(
                                    s.Db,
                                    b.Id,
                                    new MaterialInput
                                    {
                                        AuthorBiography =
                                            "Chưa có tiểu sử đã xác minh của "
                                            + b.Author
                                            + ". Thủ thư có thể bổ sung thông tin và nguồn tham khảo tại đây.",
                                        WorkIntroduction =
                                            b.Description
                                            + "\n\nBản đọc điện tử hiện tại chỉ minh họa chức năng, không phải toàn văn tác phẩm “"
                                            + b.Title
                                            + "”.",
                                        PublisherInformation =
                                            b.Publisher
                                            + " là thông tin nhà xuất bản trong bản ghi demo. Chưa có giới thiệu hoặc địa chỉ liên hệ đã xác minh.",
                                        PreviewText = notice + opening,
                                        FullText = full,
                                        IsPublished = true,
                                        IsDemo = true,
                                    }
                                );
                            }
                        }
                        s.Db.Database.ExecuteSqlCommand(
                            "INSERT dbo.LibraryFeatureVersions(Feature,AppliedAt) VALUES(N'BookReader-v1',SYSUTCDATETIME())"
                        );
                    }
                    return true;
                });
            }
        }

        private static object[] Params(int id, MaterialInput i)
        {
            return new object[]
            {
                P("@id", id),
                P("@author", i.AuthorBiography ?? ""),
                P("@work", i.WorkIntroduction ?? ""),
                P("@publisher", i.PublisherInformation ?? ""),
                P("@preview", i.PreviewText ?? ""),
                P("@full", i.FullText ?? ""),
                P("@published", i.IsPublished),
                P("@demo", i.IsDemo),
            };
        }

        private static void Insert(LibraryDb db, int id, MaterialInput i)
        {
            db.Database.ExecuteSqlCommand(
                "INSERT dbo.BookMaterials(BookId,AuthorBiography,WorkIntroduction,PublisherInformation,PreviewText,FullText,IsPublished,IsDemo,UpdatedAt) VALUES(@id,@author,@work,@publisher,@preview,@full,@published,@demo,SYSUTCDATETIME())",
                Params(id, i)
            );
        }

        public static BookMaterial Save(int id, MaterialInput i)
        {
            using (var s = new LibraryService())
            {
                return s.Write(() =>
                {
                    if (!s.Db.Books.Any(x => x.Id == id))
                        throw new RuleException("Không tìm thấy sách.");
                    if (i.IsPublished && string.IsNullOrWhiteSpace(i.FullText))
                        throw new RuleException(
                            "Cần nhập nội dung sách trước khi mở chức năng đọc."
                        );
                    var current = Get(s.Db, id);
                    if (current != null && i.Version != Convert.ToBase64String(current.Version))
                        throw new RuleException(
                            "Nội dung vừa được người khác cập nhật. Tải lại trước khi lưu."
                        );
                    if (current == null)
                        Insert(s.Db, id, i);
                    else
                        s.Db.Database.ExecuteSqlCommand(
                            "UPDATE dbo.BookMaterials SET AuthorBiography=@author,WorkIntroduction=@work,PublisherInformation=@publisher,PreviewText=@preview,FullText=@full,IsPublished=@published,IsDemo=@demo,UpdatedAt=SYSUTCDATETIME() WHERE BookId=@id",
                            Params(id, i)
                        );
                    return Get(s.Db, id);
                });
            }
        }
    }
}
