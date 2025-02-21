using System.Text.Json;
using Herta.Responses.BaseResponse;
using Microsoft.AspNetCore.Http;

namespace Herta.Responses.Response
{
    public class Response : BaseResponse<object>
    // 通用响应类
    {
        public Response(object data, int httpStatusCode = StatusCodes.Status200OK, string contentType = "application/json", JsonSerializerOptions? jsonOptions = null)
        : base(httpStatusCode, data, contentType, jsonOptions)
        {
        }
    }
}
