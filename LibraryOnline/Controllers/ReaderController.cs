using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http;
using LibraryOnline.Models;
using LibraryOnline.Services;

namespace LibraryOnline.Controllers
{
    [RoutePrefix("api/books")]
    public class ReaderController : ApiController
    {
        bool Staff => User.IsInRole("Librarian") || User.IsInRole("Administrator");

        private void NoCache()
        {
            HttpContext.Current.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            HttpContext.Current.Response.Cache.SetNoStore();
            HttpContext.Current.Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
        }

        private Loan ActiveLoan(LibraryDb db, int id)
        {
            var today = DateTime.UtcNow.Date;
            return db
                .Loans.Where(x =>
                    x.BookId == id
                    && x.MemberId == User.Identity.Name
                    && x.Status == LoanStatus.Borrowed
                    && x.ReturnedAt == null
                    && x.DueAt >= today
                )
                .OrderByDescending(x => x.DueAt)
                .FirstOrDefault();
        }

        [HttpGet, Route("{id:int}/information")]
        public IHttpActionResult Information(int id)
        {
            NoCache();
            using (var db = new LibraryDb())
            {
                var b = db.Books.Find(id);
                if (b == null || (!b.IsActive && !Staff))
                    return NotFound();
                var m = BookContentStore.Get(db, id);
                var loan = User.Identity.IsAuthenticated ? ActiveLoan(db, id) : null;
                bool published =
                    m != null && m.IsPublished && !string.IsNullOrWhiteSpace(m.FullText);
                return Ok(
                    new
                    {
                        bookId = id,
                        author = b.Author,
                        publisher = b.Publisher,
                        authorBiography = m?.AuthorBiography ?? "",
                        workIntroduction = m?.WorkIntroduction ?? b.Description,
                        publisherInformation = m?.PublisherInformation ?? "",
                        hasPreview = m != null
                            && m.IsPublished
                            && !string.IsNullOrWhiteSpace(m.PreviewText),
                        hasDigitalContent = published,
                        isDemo = m?.IsDemo ?? false,
                        canRead = published && (Staff || loan != null),
                        dueAt = loan?.DueAt,
                    }
                );
            }
        }

        [HttpGet, Route("{id:int}/preview")]
        public IHttpActionResult Preview(int id)
        {
            NoCache();
            using (var db = new LibraryDb())
            {
                var b = db.Books.Find(id);
                var m = BookContentStore.Get(db, id);
                if (
                    b == null
                    || !b.IsActive
                    || m == null
                    || !m.IsPublished
                    || string.IsNullOrWhiteSpace(m.PreviewText)
                )
                    return NotFound();
                return Ok(
                    new
                    {
                        bookId = id,
                        title = b.Title,
                        author = b.Author,
                        text = m.PreviewText,
                        isDemo = m.IsDemo,
                    }
                );
            }
        }

        [Authorize, HttpGet, Route("{id:int}/reader")]
        public IHttpActionResult Read(int id, int chapter = 1)
        {
            NoCache();
            using (var db = new LibraryDb())
            {
                var b = db.Books.Find(id);
                if (b == null)
                    return NotFound();
                var loan = ActiveLoan(db, id);
                if (!Staff && loan == null)
                    return Content(
                        HttpStatusCode.Forbidden,
                        new
                        {
                            message = "Bạn cần nhận sách và có phiếu mượn chưa quá hạn để đọc toàn bộ nội dung. Phiếu chỉ đặt, đã trả hoặc đã hủy không được cấp quyền đọc.",
                        }
                    );
                var m = BookContentStore.Get(db, id);
                if (m == null || !m.IsPublished || string.IsNullOrWhiteSpace(m.FullText))
                    return Content(
                        HttpStatusCode.NotFound,
                        new { message = "Sách chưa có nội dung điện tử được xuất bản." }
                    );
                var chapters = Regex
                    .Split(m.FullText.Replace("\r\n", "\n"), @"(?m)^---[ \t]*$")
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .ToArray();
                if (chapter < 1 || chapter > chapters.Length)
                    return BadRequest("Chương không hợp lệ.");
                return Ok(
                    new
                    {
                        bookId = id,
                        title = b.Title,
                        author = b.Author,
                        chapter,
                        totalChapters = chapters.Length,
                        chapters = chapters
                            .Select(
                                (x, i) =>
                                    new
                                    {
                                        number = i + 1,
                                        title = x.Split('\n')
                                            .First()
                                            .Substring(
                                                0,
                                                Math.Min(120, x.Split('\n').First().Length)
                                            ),
                                    }
                            )
                            .ToArray(),
                        text = chapters[chapter - 1],
                        isDemo = m.IsDemo,
                        dueAt = loan?.DueAt,
                        staffAccess = Staff,
                    }
                );
            }
        }

        [Authorize(Roles = "Administrator,Librarian"), HttpGet, Route("{id:int}/material")]
        public IHttpActionResult Material(int id)
        {
            NoCache();
            using (var db = new LibraryDb())
            {
                if (!db.Books.Any(x => x.Id == id))
                    return NotFound();
                return Ok(BookContentStore.Get(db, id) ?? new BookMaterial { BookId = id });
            }
        }

        [Authorize(Roles = "Administrator,Librarian"), HttpPut, Route("{id:int}/material")]
        public IHttpActionResult Save(int id, MaterialInput input)
        {
            NoCache();
            var m = BookContentStore.Save(id, input);
            return Ok(
                new
                {
                    m.BookId,
                    m.Version,
                    m.UpdatedAt,
                }
            );
        }
    }
}
