using Microsoft.AspNetCore.Http;
using Herta.Responses.BaseResponse;
using System.Text.Json;
using System.Collections.Generic;

namespace Herta.Responses.JsonResponse
{
    public class JsonResponse : BaseResponse<Dictionary<string, object>>
    {
        public JsonResponse(Dictionary<string, object> data, int statusCode = StatusCodes.Status200OK, string contentType = "application/json", JsonSerializerOptions? jsonOptions = null)
            : base(statusCode, data, contentType)
        {
        }
    }
}
