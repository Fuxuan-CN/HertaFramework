using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Herta.Utils.Logger;
using System.Net;
using NLog;
using System.Threading.Tasks;
using Herta.Exceptions.HttpException;

namespace Herta.Middleware.GlobalException
{
    public class GlobalExceptionHandlerMiddleware
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
                string errMsg = $"{httpException.StatusCode} {httpException.Detail} {httpException.Message}";
                string relevantStackTrace = GetRelevantStackTrace(httpException);
                string requestInfo = $"Request: {context.Request.Method} {context.Request.Path} from {context.Connection.RemoteIpAddress}";

                string errLogmsg = $"{requestInfo} - {errMsg} \n {relevantStackTrace}";
                _logger.Warn(errLogmsg);

                var response = new
                {
                    message = $"{httpException.Detail}: {httpException.ErrMessage}"
                };

                context.Response.Clear();
                context.Response.StatusCode = httpException.StatusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(response);
            }
            catch (Exception ex)
            {
                string errMsg = ex.Message;
                string relevantStackTrace = GetRelevantStackTrace(ex);

                string errLogmsg = $"Unhandled exception caught: {errMsg} \n {relevantStackTrace}";
                _logger.Error(errLogmsg);

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                var response = new
                {
                    message = "An unexpected error occurred.",
                };

                await context.Response.WriteAsJsonAsync(response);
            }
        }

        private string GetRelevantStackTrace(Exception ex)
        {
            // 获取完整的堆栈跟踪
            string? fullStackTrace = ex.StackTrace;

            // 如果需要，可以进一步解析堆栈跟踪，提取关键部分
            string[] stackLines = fullStackTrace!.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (stackLines.Length > 0)
            {
                return stackLines[0]; // 返回第一行堆栈信息
            }

            return fullStackTrace; // 如果没有堆栈信息，返回完整的堆栈
        }
    }
}
