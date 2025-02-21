using System;
using System.IO;
using System.Text.Json;
using Herta.Responses.BaseResponse;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Herta.Responses.StreamResponse
{
    public class StreamResponse : BaseResponse<Stream>
    {
        public StreamResponse(Stream stream, int httpStatusCode = StatusCodes.Status200OK, string contentType = "application/octet-stream", JsonSerializerOptions? jsonOptions = null)
            : base(httpStatusCode, stream, contentType, jsonOptions)
        {
        }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            HttpContext = context.HttpContext;
            var response = HttpContext.Response;
            response.StatusCode = HttpStatusCode;
            response.ContentType = ContentType;
            response.ContentLength = Data!.Length;

            using (Data!)
            {
                await Data.CopyToAsync(response.Body, 81920); // 将流数据写入响应体
            }
        }
    }
}
