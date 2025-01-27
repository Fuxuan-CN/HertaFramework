using Microsoft.AspNetCore.Http;
using Herta.Models.BaseResponse;
using System.Text.Json;

namespace Herta.Models.JsonResponse
{
    public class JsonResponse : BaseResponse<object>
    {
        public JsonResponse(object data,int statusCode = StatusCodes.Status200OK, string contentType = "application/json", JsonSerializerOptions? jsonOptions = null)
            : base(statusCode, data, contentType, jsonOptions)
        {
        }
    }
}