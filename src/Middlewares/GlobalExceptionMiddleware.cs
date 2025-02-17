using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Herta.Utils.Logger;
using System.Net;
using NLog;
using System.Threading.Tasks;
using Herta.Exceptions.HttpException;
using Herta.Decorators.Middleware;

namespace Herta.Middleware.GlobalException
{
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
            string errMsg = $"{httpException.StatusCode} {httpException.Detail} {httpException.Message}";
            string relevantStackTrace = GetRelevantStackTrace(httpException);
            string requestInfo = $"Request: {context.Request.Method} {context.Request.Path} from {context.Connection.RemoteIpAddress}";

            string errLogmsg = $"{requestInfo} - {errMsg} \n {relevantStackTrace}";
            _logger.Warn(errLogmsg);

            var response = new
            {
                message = httpException.ErrMessage ?? $"{httpException.Detail}: {httpException.ErrMessage}"
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

            string errLogmsg = $"Unhandled exception caught: {ex.GetType().Name}: {errMsg} \n {relevantStackTrace}";
            _logger.Error(errLogmsg);

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new
            {
                message = "An unexpected error occurred."
            };

            await context.Response.WriteAsJsonAsync(response);
        }

        private string GetRelevantStackTrace(Exception ex)
        {
            string? fullStackTrace = ex.StackTrace;
            string[] stackLines = fullStackTrace!.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return stackLines.Length > 0 ? stackLines[0] : fullStackTrace;
        }
    }
}