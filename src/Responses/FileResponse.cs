using System;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Herta.Responses.BaseResponse;

namespace Herta.Responses.FileResponse
{
    public class FileResponse : BaseResponse<Stream>
    {
        public string FilePath { get; }
        public string FileName { get; }

        public FileResponse(string filePath, string fileName, int httpStatusCode = StatusCodes.Status200OK, string contentType = "application/octet-stream", JsonSerializerOptions? jsonOptions = null)
            : base(httpStatusCode, null, contentType, jsonOptions)
        {
            FilePath = filePath;
            FileName = fileName;
        }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            HttpContext = context.HttpContext;
            var response = HttpContext.Response;
            response.StatusCode = HttpStatusCode;
            response.ContentType = ContentType;
            response.Headers.Append("Content-Disposition", $"attachment; filename=\"{FileName}\"");
            response.Headers.Append("Content-Length", new FileInfo(FilePath).Length.ToString());

            // 打开文件流并写入响应体
            using (var fileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
            {
                await fileStream.CopyToAsync(response.Body, 81920); // 80KB
            }
        }
    }
}
