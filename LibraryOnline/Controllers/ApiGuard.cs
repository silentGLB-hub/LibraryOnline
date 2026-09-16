using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Helpers;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using LibraryOnline.Services;

namespace LibraryOnline
{
    public class ApiGuard : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext c)
        {
            if (c.Request.Method != HttpMethod.Get)
            {
                try
                {
                    var cookie = HttpContext
                        .Current
                        .Request
                        .Cookies[AntiForgeryConfig.CookieName]
                        ?.Value;
                    var token = c.Request.Headers.Contains("X-CSRF-Token")
                        ? c.Request.Headers.GetValues("X-CSRF-Token").First()
                        : null;
                    AntiForgery.Validate(cookie, token);
                }
                catch
                {
                    c.Response = c.Request.CreateResponse(
                        HttpStatusCode.Forbidden,
                        new { message = "Phiên đã thay đổi. Tải lại trang rồi thử lại." }
                    );
                    return;
                }
                if (c.ActionArguments.Any(x => x.Value == null) || !c.ModelState.IsValid)
                {
                    c.Response = c.Request.CreateResponse(
                        HttpStatusCode.BadRequest,
                        new
                        {
                            message = "Dữ liệu không hợp lệ.",
                            errors = c
                                .ModelState.Values.SelectMany(v => v.Errors)
                                .Select(e => e.ErrorMessage),
                        }
                    );
                    return;
                }
            }
            try
            {
                using (var s = new LibraryService())
                    s.Sweep();
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.TraceError(e.ToString());
                c.Response = c.Request.CreateResponse(
                    HttpStatusCode.ServiceUnavailable,
                    new { message = "Không thể cập nhật dữ liệu thư viện. Vui lòng thử lại." }
                );
            }
        }

        public override void OnActionExecuted(HttpActionExecutedContext c)
        {
            if (c.Exception == null)
                return;
            var rule = c.Exception as RuleException;
            var status =
                rule != null ? HttpStatusCode.Conflict : HttpStatusCode.InternalServerError;
            System.Diagnostics.Trace.TraceError(c.Exception.ToString());
            c.Response = c.Request.CreateResponse(
                status,
                new
                {
                    message = rule?.Message
                        ?? "Không thể hoàn tất thao tác. Hãy tải lại dữ liệu và thử lại.",
                }
            );
        }
    }
}
