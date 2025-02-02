
using System;
using Herta.Exceptions.HertaException;

namespace Herta.Exceptions.HttpException
{
    // HttpException 用于再控制器或者Service层中抛出, 会自动被GlobalExceptionMiddleware捕获并返回Http错误码和错误信息
    public class HttpException : HertaBaseException
    // raise when there is an error with the http request or response
    {
        public int StatusCode { get; }
        public string? Detail { get; }
        public object? ErrMessage { get; }
        public Exception? Cause { get; }
        public HttpException(int statusCode, object? message = null, object? detail = null, Exception? cause = null)
            : base(message?.ToString() ?? string.Empty, cause)
        {
            StatusCode = statusCode;
            Detail = detail?.ToString() ?? null;
            ErrMessage = message?.ToString() ?? null;
            Cause = cause;
        }
    }
}
