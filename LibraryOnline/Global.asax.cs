using System;
using System.Data.Entity;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using LibraryOnline.Models;
using LibraryOnline.Services;

namespace LibraryOnline
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            Database.SetInitializer(new DemoInitializer());
            using (var db = new LibraryDb())
                db.Database.Initialize(false);
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(c =>
            {
                c.MapHttpAttributeRoutes();
                c.Formatters.Remove(c.Formatters.XmlFormatter);
                c.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling = Newtonsoft
                    .Json
                    .ReferenceLoopHandling
                    .Ignore;
                c.Formatters.JsonFormatter.SerializerSettings.Converters.Add(
                    new Newtonsoft.Json.Converters.StringEnumConverter()
                );
                c.Filters.Add(new ApiGuard());
            });
            RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            RouteTable.Routes.MapRoute(
                "Reports",
                "Reports/Export",
                new { controller = "Reports", action = "Export" }
            );
            RouteTable.Routes.MapRoute(
                "Pages",
                "{page}",
                new
                {
                    controller = "Home",
                    action = "Index",
                    page = "Catalog",
                }
            );
            Maintenance.Start();
        }

        protected void Application_PostAuthenticateRequest()
        {
            Context.User = new GenericPrincipal(new GenericIdentity(""), new string[0]);
            var cookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (cookie == null)
                return;
            try
            {
                var t = FormsAuthentication.Decrypt(cookie.Value);
                if (t == null || t.Expired)
                    return;
                using (var db = new LibraryDb())
                {
                    var u = db.Users.Find(t.Name);
                    if (u == null || !u.IsActive || u.SecurityStamp != t.UserData)
                        return;
                    var roles = (
                        from ur in db.Set<Microsoft.AspNet.Identity.EntityFramework.IdentityUserRole>()
                        join r in db.Roles on ur.RoleId equals r.Id
                        where ur.UserId == u.Id
                        select r.Name
                    ).ToArray();
                    Context.User = new GenericPrincipal(new GenericIdentity(u.Id, "Forms"), roles);
                }
            }
            catch { }
        }

        protected void Application_EndRequest()
        {
            if (Request.Path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
                Response.SuppressFormsAuthenticationRedirect = true;
        }
    }
}
