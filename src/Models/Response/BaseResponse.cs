using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Text.Json;

namespace Herta.Models.BaseResponse
{
    public class BaseResponse<T> : IActionResult
    // 响应类的基类
    {
        public int HttpStatusCode { get; set; }
        public T? Data { get; set; }
        public string ContentType { get; set; }
        protected HttpContext? HttpContext { get; set; }

        private static readonly JsonSerializerOptions DefaultJsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true // 可选：美化 JSON 输出
        };

        public BaseResponse(int httpStatusCode, T? data, string contentType = "application/json", JsonSerializerOptions? jsonOptions = null)
        {
            Data = data;
            HttpStatusCode = httpStatusCode;
            ContentType = contentType;
            JsonOptions = jsonOptions ?? DefaultJsonOptions;
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
                await response.WriteAsync(JsonSerializer.Serialize(Data, JsonOptions));
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