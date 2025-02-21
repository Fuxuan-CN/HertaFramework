using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Threading.Tasks;
using Herta.Responses.BaseResponse;

namespace Herta.Responses.RedirectResponse
{
    public class RedirectResponse : BaseResponse<object>
    {
        public string RedirectUrl { get; }

        public RedirectResponse(string redirectUrl, int statusCode = StatusCodes.Status302Found)
            : base(statusCode, null, "text/html; charset=utf-8") // 重定向通常不需要返回内容
        {
            RedirectUrl = redirectUrl;
        }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;
            response.StatusCode = HttpStatusCode;
            response.Headers["Location"] = RedirectUrl;

            // 如果是 302 或 303，通常不需要返回任何内容
            if (HttpStatusCode == StatusCodes.Status302Found || HttpStatusCode == StatusCodes.Status303SeeOther)
            {
                return;
            }

            // 如果是 301 永久重定向，可以返回一个简单的 HTML 提示
            if (HttpStatusCode == StatusCodes.Status301MovedPermanently)
            {
                await response.WriteAsync($"<html><body><p>Redirecting to <a href=\"{RedirectUrl}\">{RedirectUrl}</a></p></body></html>");
            }
        }
    }
}
