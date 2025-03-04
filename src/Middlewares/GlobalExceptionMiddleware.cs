using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Herta.Utils.Logger;
using System.Net;
using NLog;
using System.Threading.Tasks;
using Herta.Exceptions.HttpException;
using Herta.Decorators.Middleware;

namespace Herta.Middleware.GlobalException;

/*
全局异常处理中间件，再任意一个控制器抛出 Exception 时，会被此中间件捕获并处理
如果抛出的是 HttpException，则会返回对应的 Http 状态码和错误信息
否则，会返回 500 状态码和错误信息
*/
[Middleware(Order = 1)]
public sealed class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(GlobalExceptionHandlerMiddleware));

    public GlobalExceptionHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (HttpException httpException) when (httpException.StatusCode >= 400)
        {
            await HandleHttpException(context, httpException);
        }
        catch (Exception ex)
        {
            await HandleGeneralException(context, ex);
        }
    }

    private async Task HandleHttpException(HttpContext context, HttpException httpException)
    {
        Exception? innerException = httpException.InnerException;
        if (innerException is not null)
        {
            string extraStackTrace = innerException.StackTrace!;
            string InnerMsg = innerException.Message;
            string ErrMsg = $"Http error occurred: detail: ({httpException.Detail}), Caused by: {innerException.GetType().Name} Inner message: {InnerMsg}\n, and StackTrace: \n{extraStackTrace}";
            _logger.Error(ErrMsg);
        }
        string errMsg = $"{httpException.StatusCode} {httpException.Detail} {httpException.Message}";
        string requestInfo = $"Request: {context.Request.Method} {context.Request.Path} from {context.Connection.RemoteIpAddress}";

        var response = new
        {
            message = httpException.ErrMessage != null? httpException.ErrMessage : $"{httpException.Detail}: {httpException.ErrMessage}"
        };

        context.Response.Clear();
        context.Response.StatusCode = httpException.StatusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(response);
    }

    private async Task HandleGeneralException(HttpContext context, Exception ex)
    {
        string errMsg = ex.Message;
        string relevantStackTrace = ex.StackTrace!;

        string errLogMessage = $"Unhandled exception caught: {ex.GetType().Name}: {errMsg} \n {relevantStackTrace}";
        _logger.Error(errLogMessage);

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var response = new
        {
            message = "An unexpected error occurred."
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}
