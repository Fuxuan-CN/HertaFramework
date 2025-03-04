using Herta.Responses.BaseResponse;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Herta.Responses.TextResponse;

public class TextResponse : BaseResponse<string>
{
    public TextResponse(string text, int httpStatusCode = StatusCodes.Status200OK, string contentType = "text/plain; charset=utf-8")
        : base(httpStatusCode, text, contentType)
    {
    }

    public override async Task ExecuteResultAsync(ActionContext context)
    {
        HttpContext = context.HttpContext;
        var response = HttpContext.Response;
        response.StatusCode = HttpStatusCode;
        response.ContentType = ContentType;

        if (Data != null)
        {
            await response.WriteAsync(Data);
        }
    }
}
