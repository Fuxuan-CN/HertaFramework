using Herta.Responses.BaseResponse;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Herta.Responses.HtmlResponse;

public class HtmlResponse : BaseResponse<string>
{
    public string? HtmlContent { get; set; }

    public HtmlResponse(string htmlContent, int httpStatusCode = StatusCodes.Status200OK)
        : base(httpStatusCode, null, "text/html; charset=utf-8")
    {
        HtmlContent = htmlContent ?? string.Empty;
    }

    public override async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        await response.WriteAsync(HtmlContent!);
    }
}
