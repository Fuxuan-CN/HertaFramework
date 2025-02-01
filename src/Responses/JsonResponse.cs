using Microsoft.AspNetCore.Http;
using Herta.Responses.BaseResponse;
using System.Text.Json;

namespace Herta.Responses.JsonResponse
{
    public class JsonResponse : BaseResponse<object>
    {
        public JsonResponse(object data, int statusCode = StatusCodes.Status200OK, string contentType = "application/json", JsonSerializerOptions? jsonOptions = null)
            : base(statusCode, data, contentType, jsonOptions)
        {
        }
    }
}
