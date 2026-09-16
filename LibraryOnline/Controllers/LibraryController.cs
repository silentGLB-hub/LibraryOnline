using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;
using LibraryOnline.Models;
using LibraryOnline.Services;
using Microsoft.AspNet.Identity;

namespace LibraryOnline.Controllers
{
    [RoutePrefix("api")]
    public class LibraryController : ApiController
    {
        bool Staff => User.IsInRole("Administrator") || User.IsInRole("Librarian");
        string Me => User.Identity.Name;

        [HttpGet, Route("books")]
        public object Books(
            string q = "",
            int? genre = null,
            string availability = "",
            string author = "",
            string publisher = "",
            int page = 1,
            int pageSize = 12,
            bool manage = false
        )
        {
            using (var db = new LibraryDb())
            {
                var books = db.Books.Include(x => x.Category).AsQueryable();
                if (!manage || !Staff)
                    books = books.Where(x => x.IsActive);
                if (!string.IsNullOrWhiteSpace(q))
                    books = books.Where(x =>
                        x.Title.Contains(q) || x.Author.Contains(q) || x.ISBN.Contains(q)
                    );
                if (genre.HasValue)
                    books = books.Where(x => x.CategoryId == genre);
                if (!string.IsNullOrWhiteSpace(author))
                    books = books.Where(x => x.Author.Contains(author));
                if (!string.IsNullOrWhiteSpace(publisher))
                    books = books.Where(x => x.Publisher.Contains(publisher));
                if (availability == "available")
                    books = books.Where(x => x.AvailableCopies > 0);
                if (availability == "unavailable")
                    books = books.Where(x => x.AvailableCopies == 0);
                page = Math.Max(1, page);
                pageSize = Math.Max(1, Math.Min(100, pageSize));
                return new
                {
                    total = books.Count(),
                    page,
                    pageSize,
                    items = books
                        .OrderBy(x => x.Id)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList(),
                };
            }
        }

        [HttpGet, Route("books/{id:int}")]
        public IHttpActionResult Book(int id)
        {
            using (var db = new LibraryDb())
            {
                var b = db.Books.Include(x => x.Category).SingleOrDefault(x => x.Id == id);
                if (b == null || (!b.IsActive && !Staff))
                    return NotFound();
                return Ok(
                    new
                    {
                        book = b,
                        related = db
                            .Books.Include(x => x.Category)
                            .Where(x => x.IsActive && x.Id != id && x.CategoryId == b.CategoryId)
                            .Take(4)
                            .ToList(),
                    }
                );
            }
        }

        [Authorize(Roles = "Administrator,Librarian"), HttpPost, Route("books")]
        public object CreateBook(BookInput input)
        {
            return SaveBook(null, input);
        }

        [Authorize(Roles = "Administrator,Librarian"), HttpPut, Route("books/{id:int}")]
        public object UpdateBook(int id, BookInput input)
        {
            return SaveBook(id, input);
        }

        object SaveBook(int? id, BookInput i)
        {
            using (var s = new LibraryService())
            {
                return s.Write(() =>
                {
                    if (!s.Db.Categories.Any(x => x.Id == i.CategoryId))
                        throw new RuleException("Thể loại không tồn tại.");
                    if (s.Db.Books.Any(x => x.ISBN == i.ISBN && x.Id != id))
                        throw new RuleException("ISBN đã tồn tại.");
                    if (
                        !string.IsNullOrWhiteSpace(i.CoverImage)
                        && !(
                            i.CoverImage.StartsWith("/Content/covers/")
                            || Uri.TryCreate(i.CoverImage, UriKind.Absolute, out var uri)
                                && uri.Scheme == "https"
                        )
                    )
                        throw new RuleException(
                            "Ảnh bìa cần URL HTTPS hoặc đường dẫn /Content/covers/."
                        );
                    var b = id.HasValue ? s.Db.Books.Find(id) : new Book();
                    if (b == null)
                        throw new RuleException("Không tìm thấy sách.");
                    if (id.HasValue && i.Version != Convert.ToBase64String(b.RowVersion))
                        throw new RuleException("Sách vừa được cập nhật. Tải lại trước khi sửa.");
                    int held = b.TotalCopies - b.AvailableCopies;
                    if (i.TotalCopies < held)
                        throw new RuleException("Tổng bản không được ít hơn số đang mượn/đặt.");
                    b.Title = i.Title.Trim();
                    b.Author = i.Author.Trim();
                    b.Publisher = i.Publisher.Trim();
                    b.PublicationYear = i.PublicationYear;
                    b.ISBN = i.ISBN;
                    b.Description = i.Description;
                    b.CoverImage = string.IsNullOrWhiteSpace(i.CoverImage)
                        ? "/Content/covers/default.svg"
                        : i.CoverImage;
                    b.CategoryId = i.CategoryId;
                    b.TotalCopies = i.TotalCopies;
                    b.AvailableCopies = i.TotalCopies - held;
                    b.IsActive = i.IsActive;
                    if (!id.HasValue)
                        s.Db.Books.Add(b);
                    return b;
                });
            }
        }

        [Authorize(Roles = "Administrator,Librarian"), HttpDelete, Route("books/{id:int}")]
        public object DeleteBook(int id)
        {
            using (var s = new LibraryService())
            {
                return s.Write(() =>
                {
                    var b = s.Db.Books.Find(id);
                    if (b == null)
                        throw new RuleException("Không tìm thấy sách.");
                    if (
                        s.Db.Loans.Any(x => x.BookId == id)
                        || s.Db.ReadingListItems.Any(x => x.BookId == id)
                    )
                    {
                        b.IsActive = false;
                        return new { message = "Đã lưu trữ sách để giữ lịch sử." };
                    }
                    s.Db.Books.Remove(b);
                    return new { message = "Đã xóa sách." };
                });
            }
        }

        [HttpGet, Route("categories")]
        public object Categories()
        {
            using (var db = new LibraryDb())
                return db.Categories.OrderBy(x => x.Name).ToList();
        }

        [Authorize(Roles = "Administrator"), HttpPost, Route("categories")]
        public object CreateCategory(NameInput input)
        {
            return SaveCategory(null, input);
        }

        [Authorize(Roles = "Administrator"), HttpPut, Route("categories/{id:int}")]
        public object UpdateCategory(int id, NameInput input)
        {
            return SaveCategory(id, input);
        }

        object SaveCategory(int? id, NameInput input)
        {
            using (var s = new LibraryService())
            {
                return s.Write(() =>
                {
                    if (
                        string.IsNullOrWhiteSpace(input.Name)
                        || s.Db.Categories.Any(x => x.Name == input.Name.Trim() && x.Id != id)
                    )
                        throw new RuleException("Tên thể loại trống hoặc bị trùng.");
                    var c = id.HasValue ? s.Db.Categories.Find(id) : new Category();
                    if (c == null)
                        throw new RuleException("Không tìm thấy thể loại.");
                    c.Name = input.Name.Trim();
                    if (!id.HasValue)
                        s.Db.Categories.Add(c);
                    return c;
                });
            }
        }

        [Authorize(Roles = "Administrator"), HttpDelete, Route("categories/{id:int}")]
        public object DeleteCategory(int id)
        {
            using (var s = new LibraryService())
            {
                return s.Write(() =>
                {
                    var c = s.Db.Categories.Find(id);
                    if (c == null)
                        throw new RuleException("Không tìm thấy thể loại.");
                    if (
                        s.Db.Books.Any(x => x.CategoryId == id)
                        || s.Db.GenrePreferences.Any(x => x.CategoryId == id)
                    )
                        throw new RuleException("Thể loại đang được sử dụng.");
                    s.Db.Categories.Remove(c);
                    return new { message = "Đã xóa thể loại." };
                });
            }
        }

        [Authorize, HttpGet, Route("loans")]
        public object Loans()
        {
            using (var db = new LibraryDb())
            {
                var query = db.Loans.Include(x => x.Book).Include(x => x.Member).AsQueryable();
                if (!Staff)
                    query = query.Where(x => x.MemberId == Me);
                return query
                    .OrderByDescending(x => x.Id)
                    .ToList()
                    .Select(x => new
                    {
                        x.Id,
                        x.BookId,
                        title = x.Book.Title,
                        member = x.Member.FullName,
                        x.MemberId,
                        x.Status,
                        x.ReservedAt,
                        x.PickupExpiresAt,
                        x.BorrowedAt,
                        x.DueAt,
                        x.ReturnedAt,
                        x.RenewalCount,
                    });
            }
        }

        [Authorize(Roles = "Member"), HttpPost, Route("loans")]
        public object Reserve(ReserveInput input)
        {
            using (var s = new LibraryService())
            {
                var x = s.Reserve(Me, input.BookId);
                return new
                {
                    x.Id,
                    x.BookId,
                    x.MemberId,
                    x.Status,
                    x.ReservedAt,
                    x.PickupExpiresAt,
                };
            }
        }

        [Authorize, HttpPost, Route("loans/{id:int}/actions")]
        public object ChangeLoan(int id, ActionInput input)
        {
            using (var s = new LibraryService())
            {
                var x = s.ChangeLoan(id, input.Action, Me, Staff);
                return new
                {
                    x.Id,
                    x.Status,
                    x.BorrowedAt,
                    x.DueAt,
                    x.ReturnedAt,
                };
            }
        }

        [Authorize, HttpGet, Route("renewals")]
        public object Renewals()
        {
            using (var db = new LibraryDb())
            {
                var q = db
                    .Renewals.Include(x => x.Loan.Book)
                    .Include(x => x.Loan.Member)
                    .AsQueryable();
                if (!Staff)
                    q = q.Where(x => x.Loan.MemberId == Me);
                return q.OrderByDescending(x => x.Id)
                    .ToList()
                    .Select(x => new
                    {
                        x.Id,
                        x.LoanId,
                        title = x.Loan.Book.Title,
                        member = x.Loan.Member.FullName,
                        x.Status,
                        x.Note,
                        x.RequestedAt,
                        x.DecidedAt,
                    });
            }
        }

        [Authorize(Roles = "Member"), HttpPost, Route("loans/{id:int}/renewals")]
        public object Renewal(int id)
        {
            using (var s = new LibraryService())
            {
                var x = s.RequestRenewal(Me, id);
                return new
                {
                    x.Id,
                    x.LoanId,
                    x.Status,
                    x.RequestedAt,
                };
            }
        }

        [
            Authorize(Roles = "Administrator,Librarian"),
            HttpPost,
            Route("renewals/{id:int}/decision")
        ]
        public object Decide(int id, DecisionInput input)
        {
            using (var s = new LibraryService())
            {
                var x = s.DecideRenewal(id, input.Approve, input.Note);
                return new
                {
                    x.Id,
                    x.LoanId,
                    x.Status,
                    x.Note,
                    x.DecidedAt,
                };
            }
        }

        [Authorize, HttpGet, Route("fines")]
        public object Fines()
        {
            using (var db = new LibraryDb())
            {
                var q = db
                    .Fines.Include(x => x.Loan.Book)
                    .Include(x => x.Loan.Member)
                    .AsQueryable();
                if (!Staff)
                    q = q.Where(x => x.Loan.MemberId == Me);
                return q.OrderByDescending(x => x.Id)
                    .ToList()
                    .Select(x => new
                    {
                        x.Id,
                        x.LoanId,
                        title = x.Loan.Book.Title,
                        member = x.Loan.Member.FullName,
                        x.Amount,
                        x.Paid,
                        outstanding = x.Amount - x.Paid,
                        payments = db
                            .FinePayments.Where(p => p.FineId == x.Id)
                            .OrderByDescending(p => p.PaidAt)
                            .Select(p => new
                            {
                                p.Id,
                                p.FineId,
                                p.Amount,
                                p.PaidAt,
                                p.RecordedBy,
                                p.Note,
                            })
                            .ToList(),
                    })
                    .ToList();
            }
        }

        [Authorize(Roles = "Administrator,Librarian"), HttpPost, Route("fines/{id:int}/payments")]
        public object Pay(int id, PaymentInput input)
        {
            using (var s = new LibraryService())
            {
                var x = s.Pay(id, input.Amount, Me, input.Note);
                return new
                {
                    x.Id,
                    x.FineId,
                    x.Amount,
                    x.PaidAt,
                    x.Note,
                };
            }
        }

        [Authorize, HttpGet, Route("notifications")]
        public object Notifications()
        {
            using (var db = new LibraryDb())
                return db
                    .Notifications.Where(x => x.MemberId == Me)
                    .OrderByDescending(x => x.Id)
                    .Take(100)
                    .ToList();
        }

        [Authorize, HttpPost, Route("notifications/{id:int}/read")]
        public IHttpActionResult Read(int id)
        {
            using (var db = new LibraryDb())
            {
                var n = db.Notifications.SingleOrDefault(x => x.Id == id && x.MemberId == Me);
                if (n == null)
                    return NotFound();
                n.IsRead = true;
                db.SaveChanges();
                return Ok();
            }
        }

        [Authorize(Roles = "Member"), HttpGet, Route("reading-lists")]
        public object Lists()
        {
            using (var db = new LibraryDb())
            {
                return db
                    .ReadingLists.Where(x => x.MemberId == Me)
                    .ToList()
                    .Select(x => new
                    {
                        x.Id,
                        x.Name,
                        items = db
                            .ReadingListItems.Include(y => y.Book)
                            .Where(y => y.ReadingListId == x.Id)
                            .ToList(),
                    })
                    .ToList();
            }
        }

        [Authorize(Roles = "Member"), HttpPost, Route("reading-lists")]
        public object AddList(NameInput input)
        {
            using (var s = new LibraryService())
            {
                return s.Write(() =>
                {
                    if (
                        string.IsNullOrWhiteSpace(input.Name)
                        || s.Db.ReadingLists.Count(x => x.MemberId == Me) >= 30
                    )
                        throw new RuleException("Tên trống hoặc đã đạt giới hạn 30 danh sách.");
                    var l = new ReadingList { MemberId = Me, Name = input.Name.Trim() };
                    s.Db.ReadingLists.Add(l);
                    return l;
                });
            }
        }

        [Authorize(Roles = "Member"), HttpDelete, Route("reading-lists/{id:int}")]
        public object DeleteList(int id)
        {
            using (var s = new LibraryService())
            {
                return s.Write(() =>
                {
                    var l = s.Db.ReadingLists.SingleOrDefault(x => x.Id == id && x.MemberId == Me);
                    if (l == null)
                        throw new RuleException("Không tìm thấy danh sách.");
                    s.Db.ReadingListItems.RemoveRange(
                        s.Db.ReadingListItems.Where(x => x.ReadingListId == id)
                    );
                    s.Db.ReadingLists.Remove(l);
                    return new { message = "Đã xóa danh sách." };
                });
            }
        }

        [Authorize(Roles = "Member"), HttpPost, Route("reading-lists/{id:int}/books")]
        public object AddListBook(int id, ReserveInput input)
        {
            using (var s = new LibraryService())
            {
                return s.Write(() =>
                {
                    if (
                        !s.Db.ReadingLists.Any(x => x.Id == id && x.MemberId == Me)
                        || !s.Db.Books.Any(x => x.Id == input.BookId && x.IsActive)
                    )
                        throw new RuleException("Danh sách hoặc sách không hợp lệ.");
                    if (
                        s.Db.ReadingListItems.Any(x =>
                            x.ReadingListId == id && x.BookId == input.BookId
                        )
                    )
                        throw new RuleException("Sách đã có trong danh sách.");
                    var item = new ReadingListItem { ReadingListId = id, BookId = input.BookId };
                    s.Db.ReadingListItems.Add(item);
                    return item;
                });
            }
        }

        [
            Authorize(Roles = "Member"),
            HttpDelete,
            Route("reading-lists/{id:int}/books/{bookId:int}")
        ]
        public object RemoveListBook(int id, int bookId)
        {
            using (var s = new LibraryService())
            {
                return s.Write(() =>
                {
                    var item = s
                        .Db.ReadingListItems.Include(x => x.ReadingList)
                        .SingleOrDefault(x =>
                            x.ReadingListId == id
                            && x.BookId == bookId
                            && x.ReadingList.MemberId == Me
                        );
                    if (item == null)
                        throw new RuleException("Không tìm thấy sách trong danh sách.");
                    s.Db.ReadingListItems.Remove(item);
                    return new { message = "Đã bỏ sách khỏi danh sách." };
                });
            }
        }

        [Authorize(Roles = "Member"), HttpGet, Route("recommendations")]
        public object Recommendations()
        {
            using (var db = new LibraryDb())
            {
                var genres = db
                    .GenrePreferences.Where(x => x.MemberId == Me)
                    .Select(x => x.CategoryId)
                    .Union(
                        db.Loans.Where(x => x.MemberId == Me && x.BorrowedAt != null)
                            .Select(x => x.Book.CategoryId)
                    )
                    .ToList();
                var seen = db
                    .Loans.Where(x => x.MemberId == Me && x.BorrowedAt != null)
                    .Select(x => x.BookId);
                return db
                    .Books.Include(x => x.Category)
                    .Where(x => x.IsActive && !seen.Contains(x.Id))
                    .OrderByDescending(x => genres.Contains(x.CategoryId))
                    .ThenByDescending(x => x.AvailableCopies > 0)
                    .ThenBy(x => x.Id)
                    .Take(8)
                    .ToList();
            }
        }

        [Authorize(Roles = "Administrator,Librarian"), HttpGet, Route("users")]
        public object Users()
        {
            using (var db = new LibraryDb())
            {
                var m = Accounts.Manager(db);
                return db
                    .Users.OrderBy(x => x.FullName)
                    .ToList()
                    .Select(x => new
                    {
                        x.Id,
                        x.FullName,
                        x.Email,
                        x.PhoneNumber,
                        x.IsActive,
                        x.JoinedAt,
                        role = m.GetRoles(x.Id).FirstOrDefault(),
                        activeLoans = db.Loans.Count(l =>
                            l.MemberId == x.Id
                            && (
                                l.Status == LoanStatus.Borrowed
                                || l.Status == LoanStatus.Overdue
                                || l.Status == LoanStatus.Reserved
                            )
                        ),
                    })
                    .ToList();
            }
        }

        [Authorize(Roles = "Administrator"), HttpPost, Route("users")]
        public object CreateUser(CreateUserInput input)
        {
            using (var s = new LibraryService())
            {
                return s.Write(() =>
                {
                    if (!new[] { "Member", "Librarian", "Administrator" }.Contains(input.Role))
                        throw new RuleException("Vai trò không hợp lệ.");
                    var m = Accounts.Manager(s.Db);
                    var u = new AppUser
                    {
                        UserName = input.Email,
                        Email = input.Email,
                        FullName = input.FullName,
                        LockoutEnabled = true,
                    };
                    var r = m.Create(u, input.Password);
                    if (!r.Succeeded)
                        throw new RuleException(string.Join(" ", r.Errors));
                    r = m.AddToRole(u.Id, input.Role);
                    if (!r.Succeeded)
                        throw new RuleException(string.Join(" ", r.Errors));
                    return new
                    {
                        u.Id,
                        u.Email,
                        u.FullName,
                    };
                });
            }
        }

        [Authorize(Roles = "Administrator,Librarian"), HttpPut, Route("users/{id}")]
        public object UpdateUser(string id, UserInput input)
        {
            using (var s = new LibraryService())
            {
                return s.Write(() =>
                {
                    if (!new[] { "Member", "Librarian", "Administrator" }.Contains(input.Role))
                        throw new RuleException("Vai trò không hợp lệ.");
                    var m = Accounts.Manager(s.Db);
                    var u = s.Db.Users.Find(id);
                    if (u == null)
                        throw new RuleException("Không tìm thấy tài khoản.");
                    var roles = m.GetRoles(id);
                    if (
                        !User.IsInRole("Administrator")
                        && (!roles.Contains("Member") || input.Role != "Member")
                    )
                        throw new RuleException("Thủ thư chỉ được khóa/mở tài khoản thành viên.");
                    if (id == Me && (input.Role != "Administrator" || !input.IsActive))
                        throw new RuleException(
                            "Không thể tự khóa hoặc hạ quyền quản trị viên hiện tại."
                        );
                    if (
                        roles.Contains("Member")
                        && input.Role != "Member"
                        && s.Db.Loans.Any(x =>
                            x.MemberId == id
                            && (
                                x.Status == LoanStatus.Borrowed
                                || x.Status == LoanStatus.Overdue
                                || x.Status == LoanStatus.Reserved
                            )
                        )
                    )
                        throw new RuleException(
                            "Cần đóng các phiếu đang hoạt động trước khi đổi vai trò."
                        );
                    u.IsActive = input.IsActive;
                    u.SecurityStamp = Guid.NewGuid().ToString();
                    var r = m.RemoveFromRoles(id, roles.ToArray());
                    if (!r.Succeeded)
                        throw new RuleException(string.Join(" ", r.Errors));
                    r = m.AddToRole(id, input.Role);
                    if (!r.Succeeded)
                        throw new RuleException(string.Join(" ", r.Errors));
                    return new { message = "Đã cập nhật tài khoản." };
                });
            }
        }

        [Authorize(Roles = "Administrator,Librarian"), HttpGet, Route("policy")]
        public object Policy()
        {
            using (var db = new LibraryDb())
                return db.Policies.Single();
        }

        [Authorize(Roles = "Administrator"), HttpPut, Route("policy")]
        public object SavePolicy(PolicyInput i)
        {
            using (var s = new LibraryService())
            {
                return s.Write(() =>
                {
                    if (decimal.Truncate(i.FinePerDay) != i.FinePerDay)
                        throw new RuleException("Mức phạt VND phải là số nguyên.");
                    var p = s.Policy;
                    p.MaxActiveLoans = i.MaxActiveLoans;
                    p.LoanDays = i.LoanDays;
                    p.PickupDays = i.PickupDays;
                    p.RenewalDays = i.RenewalDays;
                    p.MaxRenewals = i.MaxRenewals;
                    p.FinePerDay = i.FinePerDay;
                    p.LibraryName = i.LibraryName;
                    return p;
                });
            }
        }

        [Authorize(Roles = "Administrator,Librarian"), HttpGet, Route("dashboard")]
        public object Dashboard()
        {
            using (var db = new LibraryDb())
            {
                var loans = db.Loans.Include(x => x.Book.Category).ToList();
                var borrowed = loans.Where(x => x.BorrowedAt != null).ToList();
                return new
                {
                    books = db.Books.Count(),
                    copies = db.Books.Sum(x => x.TotalCopies),
                    available = db.Books.Sum(x => x.AvailableCopies),
                    members = db.Set<Microsoft.AspNet.Identity.EntityFramework.IdentityUserRole>()
                        .Count(x =>
                            x.RoleId == db.Roles.FirstOrDefault(r => r.Name == "Member").Id
                        ),
                    active = loans.Count(x =>
                        x.Status == LoanStatus.Borrowed || x.Status == LoanStatus.Overdue
                    ),
                    overdue = loans.Count(x => x.Status == LoanStatus.Overdue),
                    overdueRate = borrowed.Count == 0
                        ? 0
                        : Math.Round(
                            100.0
                                * loans.Count(x => x.Status == LoanStatus.Overdue)
                                / borrowed.Count,
                            1
                        ),
                    fineCollected = db.FinePayments.Select(x => (decimal?)x.Amount).Sum() ?? 0,
                    outstanding = db.Fines.Select(x => (decimal?)(x.Amount - x.Paid)).Sum() ?? 0,
                    activeMembers = borrowed.Select(x => x.MemberId).Distinct().Count(),
                    popularBooks = borrowed
                        .GroupBy(x => x.Book.Title)
                        .Select(g => new { name = g.Key, count = g.Count() })
                        .OrderByDescending(x => x.count)
                        .Take(7),
                    genres = borrowed
                        .GroupBy(x => x.Book.Category.Name)
                        .Select(g => new { name = g.Key, count = g.Count() })
                        .OrderByDescending(x => x.count),
                    monthly = borrowed
                        .GroupBy(x => x.BorrowedAt.Value.ToString("yyyy-MM"))
                        .OrderBy(g => g.Key)
                        .Select(g => new { name = g.Key, count = g.Count() }),
                };
            }
        }
    }
}
