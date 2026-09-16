using System;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Http;
using System.Web.Security;
using LibraryOnline.Models;
using LibraryOnline.Services;
using Microsoft.AspNet.Identity;

namespace LibraryOnline.Controllers
{
    [RoutePrefix("api/session")]
    public class SessionController : ApiController
    {
        [HttpGet, Route("")]
        public object Session()
        {
            string cookie,
                token;
            AntiForgery.GetTokens(
                HttpContext.Current.Request.Cookies[AntiForgeryConfig.CookieName]?.Value,
                out cookie,
                out token
            );
            if (cookie != null)
                HttpContext.Current.Response.Cookies.Add(
                    new HttpCookie(AntiForgeryConfig.CookieName, cookie)
                    {
                        HttpOnly = true,
                        SameSite = SameSiteMode.Lax,
                        Secure = HttpContext.Current.Request.IsSecureConnection,
                    }
                );
            using (var db = new LibraryDb())
            {
                var u = User.Identity.IsAuthenticated ? db.Users.Find(User.Identity.Name) : null;
                return new
                {
                    csrfToken = token,
                    libraryName = db.Policies.Single().LibraryName,
                    user = u == null
                        ? null
                        : new
                        {
                            u.Id,
                            u.FullName,
                            u.Email,
                            u.PhoneNumber,
                            role = User.IsInRole("Administrator") ? "Administrator"
                            : User.IsInRole("Librarian") ? "Librarian"
                            : "Member",
                            genres = db
                                .GenrePreferences.Where(x => x.MemberId == u.Id)
                                .Select(x => x.CategoryId)
                                .ToArray(),
                        },
                };
            }
        }

        [HttpPost, Route("login")]
        public IHttpActionResult Login(LoginInput input)
        {
            using (var db = new LibraryDb())
            {
                var m = Accounts.Manager(db);
                var u = m.FindByEmail(input.Email);
                if (u == null || !u.IsActive || m.IsLockedOut(u.Id))
                    return Content(
                        System.Net.HttpStatusCode.Unauthorized,
                        new { message = "Thông tin đăng nhập không đúng hoặc tài khoản bị khóa." }
                    );
                if (!m.CheckPassword(u, input.Password))
                {
                    m.AccessFailed(u.Id);
                    return Content(
                        System.Net.HttpStatusCode.Unauthorized,
                        new { message = "Thông tin đăng nhập không đúng hoặc tài khoản bị khóa." }
                    );
                }
                m.ResetAccessFailedCount(u.Id);
                var ticket = new FormsAuthenticationTicket(
                    2,
                    u.Id,
                    DateTime.Now,
                    DateTime.Now.AddMinutes(60),
                    false,
                    u.SecurityStamp,
                    FormsAuthentication.FormsCookiePath
                );
                HttpContext.Current.Response.Cookies.Add(
                    new HttpCookie(
                        FormsAuthentication.FormsCookieName,
                        FormsAuthentication.Encrypt(ticket)
                    )
                    {
                        HttpOnly = true,
                        Secure = HttpContext.Current.Request.IsSecureConnection,
                        SameSite = SameSiteMode.Lax,
                        Path = "/",
                    }
                );
                return Ok(new { message = "Đăng nhập thành công." });
            }
        }

        [HttpPost, Route("register")]
        public IHttpActionResult Register(RegisterInput input)
        {
            using (var s = new LibraryService())
            {
                return s.Write(() =>
                {
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
                    r = m.AddToRole(u.Id, "Member");
                    if (!r.Succeeded)
                        throw new RuleException(string.Join(" ", r.Errors));
                    return Ok(new { message = "Đã tạo tài khoản. Bạn có thể đăng nhập." });
                });
            }
        }

        [Authorize, HttpPost, Route("logout")]
        public object Logout()
        {
            FormsAuthentication.SignOut();
            return new { message = "Đã đăng xuất." };
        }

        [Authorize, HttpPut, Route("profile")]
        public object Profile(ProfileInput input)
        {
            using (var s = new LibraryService())
            {
                return s.Write(() =>
                {
                    var u = s.Db.Users.Find(User.Identity.Name);
                    u.FullName = input.FullName;
                    u.PhoneNumber = input.PhoneNumber;
                    var genres = (input.Genres ?? new int[0]).Distinct().ToArray();
                    if (s.Db.Categories.Count(x => genres.Contains(x.Id)) != genres.Length)
                        throw new RuleException("Thể loại không hợp lệ.");
                    s.Db.GenrePreferences.RemoveRange(
                        s.Db.GenrePreferences.Where(x => x.MemberId == u.Id)
                    );
                    foreach (var g in genres)
                        s.Db.GenrePreferences.Add(
                            new GenrePreference { MemberId = u.Id, CategoryId = g }
                        );
                    return new { message = "Đã cập nhật hồ sơ." };
                });
            }
        }

        [Authorize, HttpPost, Route("password")]
        public object Password(PasswordInput input)
        {
            using (var db = new LibraryDb())
            {
                var m = Accounts.Manager(db);
                var r = m.ChangePassword(User.Identity.Name, input.OldPassword, input.NewPassword);
                if (!r.Succeeded)
                    throw new RuleException(string.Join(" ", r.Errors));
                FormsAuthentication.SignOut();
                return new { message = "Đã đổi mật khẩu. Vui lòng đăng nhập lại." };
            }
        }
    }
}
