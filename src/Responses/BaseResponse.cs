using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Text.Json;

namespace Herta.Responses.BaseResponse
{
    public class BaseResponse<T> : IActionResult
    // 响应类的基类
    {
        public int HttpStatusCode { get; set; }
        public T? Data { get; set; }
        public string ContentType { get; set; }
        protected HttpContext? HttpContext { get; set; }

        private static readonly JsonSerializerOptions DefaultJsonSettings = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        public BaseResponse(int httpStatusCode, T? data, string contentType = "application/json", JsonSerializerOptions? jsonOptions = null)
        {
            Data = data;
            HttpStatusCode = httpStatusCode;
            ContentType = contentType;
            JsonOptions = jsonOptions ?? DefaultJsonSettings;
        }

        protected HttpResponse? GetSourceResponse()
        {
            return HttpContext?.Response;
        }

        protected HttpContext? GetHttpContext()
        {
            return HttpContext;
        }

        public virtual async Task ExecuteResultAsync(ActionContext context)
        {
            HttpContext = context.HttpContext;
            var response = HttpContext.Response;
            response.StatusCode = HttpStatusCode;
            response.ContentType = ContentType;

            if (Data != null)
            {
                var dat = JsonSerializer.Serialize(Data, JsonOptions);
                await response.WriteAsync(dat);
            }
            else if (HttpStatusCode == StatusCodes.Status204NoContent)
            {
                // 如果是 204 No Content，不写入任何内容
                return;
            }
            else
            {
                // 如果没有数据，返回一个空的 JSON 对象
                await response.WriteAsync("{}");
            }
        }

        private JsonSerializerOptions JsonOptions { get; }
    }
}
