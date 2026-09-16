using System;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using LibraryOnline.Services;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace LibraryOnline.Models
{
    public class DemoInitializer : CreateDatabaseIfNotExists<LibraryDb>
    {
        protected override void Seed(LibraryDb db)
        {
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));
            foreach (var role in new[] { "Administrator", "Librarian", "Member" })
                roleManager.Create(new IdentityRole(role));
            db.Policies.Add(new LibraryPolicy());
            db.SaveChanges();
            if (ConfigurationManager.AppSettings["DemoSeedEnabled"] != "true")
                return;
            var seedPassword = ConfigurationManager.AppSettings["DemoPassword"];
            if (string.IsNullOrWhiteSpace(seedPassword))
                throw new Exception(
                    "Chạy scripts/Configure-Demo.ps1 để cấu hình mật khẩu demo cục bộ trước khi tạo database."
                );
            var manager = Accounts.Manager(db);
            Func<string, string, string, AppUser> user = (email, name, role) =>
            {
                var u = new AppUser
                {
                    UserName = email,
                    Email = email,
                    FullName = name,
                    LockoutEnabled = true,
                };
                var r = manager.Create(u, seedPassword);
                if (!r.Succeeded)
                    throw new Exception(string.Join(";", r.Errors));
                manager.AddToRole(u.Id, role);
                return u;
            };
            var admin = user("admin@library.local", "Quản trị thư viện", "Administrator");
            var lib = user("librarian01@library.local", "Nguyễn Minh Anh", "Librarian");
            user("librarian02@library.local", "Trần Bảo Ngọc", "Librarian");
            var names = new[]
            {
                "Nguyễn Hoàng Phúc",
                "Trần Minh Thư",
                "Lê Gia Huy",
                "Phạm Ngọc Anh",
                "Hoàng Đức Minh",
                "Võ Thanh Hà",
                "Đặng Bảo Trâm",
                "Bùi Anh Khoa",
                "Đỗ Khánh Linh",
                "Hồ Nhật Nam",
                "Ngô Hải Yến",
                "Dương Tuấn Kiệt",
                "Lý Phương Anh",
                "Vũ Minh Châu",
                "Đinh Quốc Bảo",
                "Mai Thảo Vy",
                "Trịnh Nhật Minh",
                "Đào Hà My",
                "Phan Thanh Tùng",
                "Lâm Bảo An",
            };
            var members = names
                .Select(
                    (n, i) =>
                        user("member" + (i + 1).ToString("00") + "@library.local", n, "Member")
                )
                .ToArray();
            var categories = new[]
            {
                "Văn học",
                "Khoa học",
                "Lịch sử",
                "Công nghệ",
                "Phát triển bản thân",
                "Kinh tế",
                "Nghệ thuật",
                "Thiếu nhi",
            }
                .Select(n => new Category { Name = n })
                .ToArray();
            db.Categories.AddRange(categories);
            db.SaveChanges();
            var books = new[]
            {
                new Book
                {
                    Title = "Dế Mèn phiêu lưu ký",
                    Author = "Tô Hoài",
                    Publisher = "NXB Trẻ",
                    PublicationYear = 2000,
                    ISBN = "9786040000001",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Dế Mèn phiêu lưu ký. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/1.svg",
                    Category = categories[0],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Tôi thấy hoa vàng trên cỏ xanh",
                    Author = "Nguyễn Nhật Ánh",
                    Publisher = "NXB Kim Đồng",
                    PublicationYear = 2001,
                    ISBN = "9786040000002",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Tôi thấy hoa vàng trên cỏ xanh. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/2.svg",
                    Category = categories[0],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Mắt biếc",
                    Author = "Nguyễn Nhật Ánh",
                    Publisher = "NXB Tri Thức",
                    PublicationYear = 2002,
                    ISBN = "9786040000003",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Mắt biếc. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/3.svg",
                    Category = categories[0],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Nhà giả kim",
                    Author = "Paulo Coelho",
                    Publisher = "NXB Tổng hợp",
                    PublicationYear = 2003,
                    ISBN = "9786040000004",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Nhà giả kim. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/4.svg",
                    Category = categories[0],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Hoàng tử bé",
                    Author = "Antoine de Saint-Exupéry",
                    Publisher = "NXB Trẻ",
                    PublicationYear = 2004,
                    ISBN = "9786040000005",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Hoàng tử bé. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/5.svg",
                    Category = categories[0],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Những người khốn khổ",
                    Author = "Victor Hugo",
                    Publisher = "NXB Kim Đồng",
                    PublicationYear = 2005,
                    ISBN = "9786040000006",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Những người khốn khổ. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/6.svg",
                    Category = categories[0],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Kiêu hãnh và định kiến",
                    Author = "Jane Austen",
                    Publisher = "NXB Tri Thức",
                    PublicationYear = 2006,
                    ISBN = "9786040000007",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Kiêu hãnh và định kiến. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/7.svg",
                    Category = categories[0],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Ông già và biển cả",
                    Author = "Ernest Hemingway",
                    Publisher = "NXB Tổng hợp",
                    PublicationYear = 2007,
                    ISBN = "9786040000008",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Ông già và biển cả. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/8.svg",
                    Category = categories[0],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Lược sử thời gian",
                    Author = "Stephen Hawking",
                    Publisher = "NXB Trẻ",
                    PublicationYear = 2008,
                    ISBN = "9786040000009",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Lược sử thời gian. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/9.svg",
                    Category = categories[1],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Vũ trụ",
                    Author = "Carl Sagan",
                    Publisher = "NXB Kim Đồng",
                    PublicationYear = 2009,
                    ISBN = "9786040000010",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Vũ trụ. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/10.svg",
                    Category = categories[1],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Nguồn gốc các loài",
                    Author = "Charles Darwin",
                    Publisher = "NXB Tri Thức",
                    PublicationYear = 2010,
                    ISBN = "9786040000011",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Nguồn gốc các loài. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/11.svg",
                    Category = categories[1],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Gen vị kỷ",
                    Author = "Richard Dawkins",
                    Publisher = "NXB Tổng hợp",
                    PublicationYear = 2011,
                    ISBN = "9786040000012",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Gen vị kỷ. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/12.svg",
                    Category = categories[1],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Vật lý của những điều tưởng chừng bất khả",
                    Author = "Michio Kaku",
                    Publisher = "NXB Trẻ",
                    PublicationYear = 2012,
                    ISBN = "9786040000013",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Vật lý của những điều tưởng chừng bất khả. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/13.svg",
                    Category = categories[1],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Sapiens",
                    Author = "Yuval Noah Harari",
                    Publisher = "NXB Kim Đồng",
                    PublicationYear = 2013,
                    ISBN = "9786040000014",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Sapiens. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/14.svg",
                    Category = categories[2],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Lịch sử Việt Nam",
                    Author = "Trần Trọng Kim",
                    Publisher = "NXB Tri Thức",
                    PublicationYear = 2014,
                    ISBN = "9786040000015",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Lịch sử Việt Nam. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/15.svg",
                    Category = categories[2],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Súng, vi trùng và thép",
                    Author = "Jared Diamond",
                    Publisher = "NXB Tổng hợp",
                    PublicationYear = 2015,
                    ISBN = "9786040000016",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Súng, vi trùng và thép. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/16.svg",
                    Category = categories[2],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Thế giới cho đến ngày hôm qua",
                    Author = "Jared Diamond",
                    Publisher = "NXB Trẻ",
                    PublicationYear = 2016,
                    ISBN = "9786040000017",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Thế giới cho đến ngày hôm qua. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/17.svg",
                    Category = categories[2],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Lược sử loài người",
                    Author = "Yuval Noah Harari",
                    Publisher = "NXB Kim Đồng",
                    PublicationYear = 2017,
                    ISBN = "9786040000018",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Lược sử loài người. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/18.svg",
                    Category = categories[2],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Clean Code",
                    Author = "Robert C. Martin",
                    Publisher = "NXB Tri Thức",
                    PublicationYear = 2018,
                    ISBN = "9786040000019",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Clean Code. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/19.svg",
                    Category = categories[3],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "The Pragmatic Programmer",
                    Author = "David Thomas",
                    Publisher = "NXB Tổng hợp",
                    PublicationYear = 2019,
                    ISBN = "9786040000020",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá The Pragmatic Programmer. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/20.svg",
                    Category = categories[3],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Design Patterns",
                    Author = "Erich Gamma",
                    Publisher = "NXB Trẻ",
                    PublicationYear = 2020,
                    ISBN = "9786040000021",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Design Patterns. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/21.svg",
                    Category = categories[3],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Introduction to Algorithms",
                    Author = "Thomas H. Cormen",
                    Publisher = "NXB Kim Đồng",
                    PublicationYear = 2021,
                    ISBN = "9786040000022",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Introduction to Algorithms. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/22.svg",
                    Category = categories[3],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Code Complete",
                    Author = "Steve McConnell",
                    Publisher = "NXB Tri Thức",
                    PublicationYear = 2022,
                    ISBN = "9786040000023",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Code Complete. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/23.svg",
                    Category = categories[3],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Refactoring",
                    Author = "Martin Fowler",
                    Publisher = "NXB Tổng hợp",
                    PublicationYear = 2023,
                    ISBN = "9786040000024",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Refactoring. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/24.svg",
                    Category = categories[3],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "ASP.NET MVC in Action",
                    Author = "Jeffrey Palermo",
                    Publisher = "NXB Trẻ",
                    PublicationYear = 2024,
                    ISBN = "9786040000025",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá ASP.NET MVC in Action. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/25.svg",
                    Category = categories[3],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Đắc nhân tâm",
                    Author = "Dale Carnegie",
                    Publisher = "NXB Kim Đồng",
                    PublicationYear = 2000,
                    ISBN = "9786040000026",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Đắc nhân tâm. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/26.svg",
                    Category = categories[4],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Tư duy nhanh và chậm",
                    Author = "Daniel Kahneman",
                    Publisher = "NXB Tri Thức",
                    PublicationYear = 2001,
                    ISBN = "9786040000027",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Tư duy nhanh và chậm. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/27.svg",
                    Category = categories[4],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Sức mạnh của thói quen",
                    Author = "Charles Duhigg",
                    Publisher = "NXB Tổng hợp",
                    PublicationYear = 2002,
                    ISBN = "9786040000028",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Sức mạnh của thói quen. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/28.svg",
                    Category = categories[4],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Atomic Habits",
                    Author = "James Clear",
                    Publisher = "NXB Trẻ",
                    PublicationYear = 2003,
                    ISBN = "9786040000029",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Atomic Habits. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/29.svg",
                    Category = categories[4],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Deep Work",
                    Author = "Cal Newport",
                    Publisher = "NXB Kim Đồng",
                    PublicationYear = 2004,
                    ISBN = "9786040000030",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Deep Work. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/30.svg",
                    Category = categories[4],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Đi tìm lẽ sống",
                    Author = "Viktor E. Frankl",
                    Publisher = "NXB Tri Thức",
                    PublicationYear = 2005,
                    ISBN = "9786040000031",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Đi tìm lẽ sống. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/31.svg",
                    Category = categories[4],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Tâm lý học về tiền",
                    Author = "Morgan Housel",
                    Publisher = "NXB Tổng hợp",
                    PublicationYear = 2006,
                    ISBN = "9786040000032",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Tâm lý học về tiền. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/32.svg",
                    Category = categories[5],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Nhà đầu tư thông minh",
                    Author = "Benjamin Graham",
                    Publisher = "NXB Trẻ",
                    PublicationYear = 2007,
                    ISBN = "9786040000033",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Nhà đầu tư thông minh. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/33.svg",
                    Category = categories[5],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Từ tốt đến vĩ đại",
                    Author = "Jim Collins",
                    Publisher = "NXB Kim Đồng",
                    PublicationYear = 2008,
                    ISBN = "9786040000034",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Từ tốt đến vĩ đại. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/34.svg",
                    Category = categories[5],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Khởi nghiệp tinh gọn",
                    Author = "Eric Ries",
                    Publisher = "NXB Tri Thức",
                    PublicationYear = 2009,
                    ISBN = "9786040000035",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Khởi nghiệp tinh gọn. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/35.svg",
                    Category = categories[5],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Quốc gia khởi nghiệp",
                    Author = "Dan Senor",
                    Publisher = "NXB Tổng hợp",
                    PublicationYear = 2010,
                    ISBN = "9786040000036",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Quốc gia khởi nghiệp. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/36.svg",
                    Category = categories[5],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Nghệ thuật tư duy rành mạch",
                    Author = "Rolf Dobelli",
                    Publisher = "NXB Trẻ",
                    PublicationYear = 2011,
                    ISBN = "9786040000037",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Nghệ thuật tư duy rành mạch. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/37.svg",
                    Category = categories[5],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Câu chuyện nghệ thuật",
                    Author = "E. H. Gombrich",
                    Publisher = "NXB Kim Đồng",
                    PublicationYear = 2012,
                    ISBN = "9786040000038",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Câu chuyện nghệ thuật. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/38.svg",
                    Category = categories[6],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Lịch sử cái đẹp",
                    Author = "Umberto Eco",
                    Publisher = "NXB Tri Thức",
                    PublicationYear = 2013,
                    ISBN = "9786040000039",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Lịch sử cái đẹp. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/39.svg",
                    Category = categories[6],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Bàn về nhiếp ảnh",
                    Author = "Susan Sontag",
                    Publisher = "NXB Tổng hợp",
                    PublicationYear = 2014,
                    ISBN = "9786040000040",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Bàn về nhiếp ảnh. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/40.svg",
                    Category = categories[6],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Nghệ thuật và thị giác",
                    Author = "Rudolf Arnheim",
                    Publisher = "NXB Trẻ",
                    PublicationYear = 2015,
                    ISBN = "9786040000041",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Nghệ thuật và thị giác. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/41.svg",
                    Category = categories[6],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Màu sắc và ý nghĩa",
                    Author = "John Gage",
                    Publisher = "NXB Kim Đồng",
                    PublicationYear = 2016,
                    ISBN = "9786040000042",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Màu sắc và ý nghĩa. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/42.svg",
                    Category = categories[6],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Không gia đình",
                    Author = "Hector Malot",
                    Publisher = "NXB Tri Thức",
                    PublicationYear = 2017,
                    ISBN = "9786040000043",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Không gia đình. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/43.svg",
                    Category = categories[7],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Alice ở xứ sở diệu kỳ",
                    Author = "Lewis Carroll",
                    Publisher = "NXB Tổng hợp",
                    PublicationYear = 2018,
                    ISBN = "9786040000044",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Alice ở xứ sở diệu kỳ. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/44.svg",
                    Category = categories[7],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Winnie-the-Pooh",
                    Author = "A. A. Milne",
                    Publisher = "NXB Trẻ",
                    PublicationYear = 2019,
                    ISBN = "9786040000045",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Winnie-the-Pooh. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/45.svg",
                    Category = categories[7],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Khu vườn bí mật",
                    Author = "Frances Hodgson Burnett",
                    Publisher = "NXB Kim Đồng",
                    PublicationYear = 2020,
                    ISBN = "9786040000046",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Khu vườn bí mật. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/46.svg",
                    Category = categories[7],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Charlie và nhà máy sô-cô-la",
                    Author = "Roald Dahl",
                    Publisher = "NXB Tri Thức",
                    PublicationYear = 2021,
                    ISBN = "9786040000047",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Charlie và nhà máy sô-cô-la. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/47.svg",
                    Category = categories[7],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Pippi tất dài",
                    Author = "Astrid Lindgren",
                    Publisher = "NXB Tổng hợp",
                    PublicationYear = 2022,
                    ISBN = "9786040000048",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Pippi tất dài. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/48.svg",
                    Category = categories[7],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Những cuộc phiêu lưu của Tom Sawyer",
                    Author = "Mark Twain",
                    Publisher = "NXB Trẻ",
                    PublicationYear = 2023,
                    ISBN = "9786040000049",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Những cuộc phiêu lưu của Tom Sawyer. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/49.svg",
                    Category = categories[7],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
                new Book
                {
                    Title = "Đảo giấu vàng",
                    Author = "Robert Louis Stevenson",
                    Publisher = "NXB Kim Đồng",
                    PublicationYear = 2024,
                    ISBN = "9786040000050",
                    Description =
                        "Bản ghi minh họa phục vụ đồ án. Khám phá Đảo giấu vàng. Thông tin ấn bản và ISBN là dữ liệu demo, không phải dữ liệu thư mục chính thức.",
                    CoverImage = "/Content/covers/50.svg",
                    Category = categories[7],
                    TotalCopies = 3,
                    AvailableCopies = 3,
                    IsActive = true,
                },
            };
            db.Books.AddRange(books);
            db.SaveChanges();
            var now = DateTime.UtcNow;
            var today = now.Date;
            for (int i = 0; i < 10; i++)
                db.Loans.Add(
                    new Loan
                    {
                        BookId = books[i].Id,
                        MemberId = members[i].Id,
                        Status = LoanStatus.Overdue,
                        ReservedAt = today.AddDays(-30 - i),
                        PickupExpiresAt = today.AddDays(-27 - i),
                        BorrowedAt = today.AddDays(-20 - i),
                        DueAt = today.AddDays(-6 - i),
                        DailyFineRate = 5000,
                    }
                );
            for (int i = 0; i < 8; i++)
                db.Loans.Add(
                    new Loan
                    {
                        BookId = books[10 + i].Id,
                        MemberId = members[i % 5].Id,
                        Status = LoanStatus.Borrowed,
                        ReservedAt = today.AddDays(-8),
                        PickupExpiresAt = today.AddDays(-5),
                        BorrowedAt = today.AddDays(-7),
                        DueAt = today.AddDays(i == 7 ? 1 : 7),
                        DailyFineRate = 5000,
                    }
                );
            for (int i = 0; i < 12; i++)
                db.Loans.Add(
                    new Loan
                    {
                        BookId = books[18 + i].Id,
                        MemberId = members[i % 15].Id,
                        Status = LoanStatus.Returned,
                        ReservedAt = today.AddDays(-50 - i * 4),
                        PickupExpiresAt = today.AddDays(-47 - i * 4),
                        BorrowedAt = today.AddDays(-49 - i * 4),
                        DueAt = today.AddDays(-35 - i * 4),
                        ReturnedAt = today.AddDays(-37 - i * 4),
                        DailyFineRate = 5000,
                    }
                );
            for (int i = 0; i < 10; i++)
                db.Loans.Add(
                    new Loan
                    {
                        BookId = books[30 + i].Id,
                        MemberId = members[10 + i % 5].Id,
                        Status = i < 8 ? LoanStatus.Reserved : LoanStatus.Cancelled,
                        ReservedAt = now.AddHours(-4),
                        PickupExpiresAt = now.AddDays(3),
                        DailyFineRate = 5000,
                    }
                );
            db.SaveChanges();
            // A second reservation blocks renewal of book 11 and demonstrates the rule.
            db.Loans.Add(
                new Loan
                {
                    BookId = books[10].Id,
                    MemberId = members[14].Id,
                    Status = LoanStatus.Reserved,
                    ReservedAt = now,
                    PickupExpiresAt = now.AddDays(3),
                    DailyFineRate = 5000,
                }
            );
            db.SaveChanges();
            foreach (var b in books)
            {
                int held = db.Loans.Count(l =>
                    l.BookId == b.Id
                    && (
                        l.Status == LoanStatus.Reserved
                        || l.Status == LoanStatus.Borrowed
                        || l.Status == LoanStatus.Overdue
                    )
                );
                if (b.Id == books[0].Id || b.Id == books[10].Id)
                    b.TotalCopies = held;
                b.AvailableCopies = b.TotalCopies - held;
            }
            var overdue = db
                .Loans.Where(x => x.Status == LoanStatus.Overdue)
                .OrderBy(x => x.Id)
                .ToList();
            for (int i = 0; i < overdue.Count; i++)
            {
                var l = overdue[i];
                db.Fines.Add(
                    new Fine
                    {
                        LoanId = l.Id,
                        Amount = (today - l.DueAt.Value.Date).Days * 5000,
                        Paid = i < 5 ? 10000 : 0,
                    }
                );
            }
            db.SaveChanges();
            foreach (var f in db.Fines.OrderBy(x => x.Id).Take(5))
                db.FinePayments.Add(
                    new FinePayment
                    {
                        FineId = f.Id,
                        Amount = 10000,
                        PaidAt = now.AddHours(-1),
                        RecordedBy = lib.Id,
                        Note = "Thanh toán mẫu tại quầy",
                    }
                );
            var active = db
                .Loans.Where(x => x.Status == LoanStatus.Borrowed)
                .OrderBy(x => x.Id)
                .ToList();
            for (int i = 0; i < 5; i++)
            {
                var l = active[i + 1];
                db.Renewals.Add(
                    new RenewalRequest
                    {
                        LoanId = l.Id,
                        RequestedAt = now.AddHours(-2),
                        Status =
                            i < 3 ? RenewalStatus.Pending
                            : i == 3 ? RenewalStatus.Approved
                            : RenewalStatus.Rejected,
                        DecidedAt = i >= 3 ? (DateTime?)now : null,
                        Note = i == 4 ? "Minh họa yêu cầu bị từ chối" : "Dữ liệu demo",
                    }
                );
                if (i == 3)
                {
                    l.DueAt = l.DueAt.Value.AddDays(7);
                    l.RenewalCount = 1;
                }
            }
            for (int i = 0; i < 5; i++)
            {
                var list = new ReadingList { MemberId = members[i].Id, Name = "Sách muốn đọc" };
                db.ReadingLists.Add(list);
                db.SaveChanges();
                db.ReadingListItems.Add(
                    new ReadingListItem { ReadingListId = list.Id, BookId = books[42 + i].Id }
                );
                db.GenrePreferences.Add(
                    new GenrePreference { MemberId = members[i].Id, CategoryId = categories[i].Id }
                );
            }
            foreach (var m in members)
                db.Notifications.Add(
                    new Notification
                    {
                        MemberId = m.Id,
                        EventKey = "welcome:" + m.Id,
                        Message =
                            "Chào mừng bạn đến Thư viện Mở. Khám phá cuốn sách tiếp theo của bạn!",
                        CreatedAt = now,
                    }
                );
            db.SaveChanges();
        }
    }
}
