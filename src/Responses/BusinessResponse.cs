using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Text.Json;
using Herta.Responses.BaseResponse;
using Herta.Models.Enums.BusinessCode;

namespace Herta.Responses.BusinessResponse;

public class BusinessResponse<T> : BaseResponse<object>
{
    private BusinessCode _code;
    private string? _message;
    private T? _data;

    public BusinessResponse(BusinessCode code, string? message, T? data = default) : base(StatusCodes.Status200OK, null, "application/json")
    {
        _code = code;
        _message = message;
        _data = data;
        SetStatusCode(_code);
    }

    private void SetStatusCode(BusinessCode code)
    {
        // 提取Http状态码，比如 40100 对应 401，实际上就是提取前三位用作状态码
        int _httpCode = (int)code / 10000;
        HttpStatusCode = (int)_httpCode == 0 ? StatusCodes.Status200OK : (int)_httpCode;
    }

    public override async Task ExecuteResultAsync(ActionContext context)
    {
        HttpContext = context.HttpContext;
        var response = GetSourceResponse();

        if (response == null)
        {
            throw new InvalidOperationException("HttpContext is not available.");
        }

        response.StatusCode = HttpStatusCode;
        response.ContentType = ContentType;

        // 构造固定的响应格式
        var result = new
        {
            code = (int)_code,
            message = _message,
            data = _data
        };

        // 序列化并写入响应体
        var json = JsonSerializer.Serialize(result);
        await response.WriteAsync(json);
    }
}
