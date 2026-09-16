using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using LibraryOnline.Models;
using LibraryOnline.Services;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace LibraryOnline.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        [AllowAnonymous]
        public ActionResult Index(string page)
        {
            var known = new[]
            {
                "Catalog",
                "Reader",
                "Account",
                "Loans",
                "Fines",
                "Reading",
                "Notifications",
                "Dashboard",
                "ManageBooks",
                "Users",
                "Settings",
                "Profile",
            };
            if (!known.Contains(page))
                return HttpNotFound();
            if (
                page != "Catalog"
                && page != "Account"
                && page != "Reader"
                && !User.Identity.IsAuthenticated
            )
                return Redirect("/Account");
            if (
                new[] { "Dashboard", "ManageBooks", "Users" }.Contains(page)
                && !User.IsInRole("Librarian")
                && !User.IsInRole("Administrator")
            )
                return new HttpStatusCodeResult(403);
            if (page == "Settings" && !User.IsInRole("Administrator"))
                return new HttpStatusCodeResult(403);
            ViewBag.Page = page;
            return View("Index");
        }
    }

    [Authorize]
    public class ReportsController : Controller
    {
        public ActionResult Export(string kind = "loans")
        {
            if (kind != "loans" && kind != "fines")
                return new HttpStatusCodeResult(400);
            using (var s = new LibraryService())
            {
                s.Sweep();
                var db = s.Db;
                bool staff = User.IsInRole("Administrator") || User.IsInRole("Librarian");
                var q = db.Loans.Include(x => x.Book).Include(x => x.Member).AsQueryable();
                if (!staff)
                    q = q.Where(x => x.MemberId == User.Identity.Name);
                var loans = q.OrderByDescending(x => x.Id).ToList();
                var doc = new PdfDocument();
                doc.Info.Title = kind == "fines" ? "Báo cáo tiền phạt" : "Lịch sử mượn sách";
                XGraphics g = null;
                double y = 0;
                int number = 0;
                var font = new XFont(
                    "Arial",
                    10,
                    XFontStyle.Regular,
                    new XPdfFontOptions(PdfFontEncoding.Unicode)
                );
                var bold = new XFont(
                    "Arial",
                    16,
                    XFontStyle.Bold,
                    new XPdfFontOptions(PdfFontEncoding.Unicode)
                );
                Action newPage = () =>
                {
                    g?.Dispose();
                    var p = doc.AddPage();
                    g = XGraphics.FromPdfPage(p);
                    g.DrawString(doc.Info.Title, bold, XBrushes.DarkSlateBlue, 35, 40);
                    g.DrawString(
                        "LibraryOnline · "
                            + DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm")
                            + " UTC · Trang "
                            + (++number),
                        font,
                        XBrushes.Gray,
                        35,
                        60
                    );
                    y = 90;
                };
                newPage();
                Action<string> line = text =>
                {
                    var words = text.Split(' ');
                    var current = "";
                    foreach (var word in words)
                    {
                        if (g.MeasureString(current + word, font).Width > 520)
                        {
                            if (y > 780)
                                newPage();
                            g.DrawString(current, font, XBrushes.Black, 35, y);
                            y += 16;
                            current = "";
                        }
                        current += word + " ";
                    }
                    if (y > 780)
                        newPage();
                    g.DrawString(current, font, XBrushes.Black, 35, y);
                    y += 18;
                };
                foreach (var l in loans)
                {
                    if (kind == "fines")
                    {
                        var f = db.Fines.SingleOrDefault(x => x.LoanId == l.Id);
                        if (f == null)
                            continue;
                        line("#" + l.Id + " | " + l.Member.FullName + " | " + l.Book.Title);
                        line(
                            "Phạt: "
                                + f.Amount.ToString("N0")
                                + " VND | Đã trả: "
                                + f.Paid.ToString("N0")
                                + " | Còn nợ: "
                                + (f.Amount - f.Paid).ToString("N0")
                        );
                    }
                    else
                    {
                        line("#" + l.Id + " | " + l.Member.FullName + " | " + l.Book.Title);
                        line(
                            l.Status
                                + " | Mượn: "
                                + (l.BorrowedAt?.ToString("dd/MM/yyyy") ?? "-")
                                + " | Hạn: "
                                + (l.DueAt?.ToString("dd/MM/yyyy") ?? "-")
                                + " | Trả: "
                                + (l.ReturnedAt?.ToString("dd/MM/yyyy") ?? "-")
                        );
                    }
                    y += 8;
                }
                if (loans.Count == 0)
                    line("Chưa có dữ liệu.");
                g.Dispose();
                using (var stream = new MemoryStream())
                {
                    doc.Save(stream, false);
                    return File(
                        stream.ToArray(),
                        "application/pdf",
                        "LibraryOnline-" + kind + ".pdf"
                    );
                }
            }
        }
    }
}
