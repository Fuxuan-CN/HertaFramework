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
        switch (code)
        {
            case BusinessCode.Success:
                HttpStatusCode = StatusCodes.Status200OK;
                break;
            case BusinessCode.ArgumentError:
                HttpStatusCode = StatusCodes.Status400BadRequest;
                break;
            case BusinessCode.Unauthorized:
                HttpStatusCode = StatusCodes.Status401Unauthorized;
                break;
            case BusinessCode.TokenFailed:
                HttpStatusCode = StatusCodes.Status401Unauthorized;
                break;
            case BusinessCode.Forbidden:
                HttpStatusCode = StatusCodes.Status403Forbidden;
                break;
            case BusinessCode.NotFound:
                HttpStatusCode = StatusCodes.Status404NotFound;
                break;
            case BusinessCode.ServerError:
                HttpStatusCode = StatusCodes.Status500InternalServerError;
                break;
            default:
                HttpStatusCode = StatusCodes.Status200OK;
                break;
        }
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
