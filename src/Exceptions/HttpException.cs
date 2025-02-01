
using System;
using Herta.Exceptions.HertaException;

namespace Herta.Exceptions.HttpException
{
    public class HttpException : HertaBaseException
    // raise when there is an error with the http request or response
    {
        public int StatusCode { get; }
        public string? Detail { get; }
        public object? ErrMessage { get; }
        public Exception? Cause { get; }
        public HttpException(int statusCode, object? detail, object? message = null, Exception? cause = null)
            : base(message?.ToString() ?? "Unknown error", cause)
        {
            StatusCode = statusCode;
            ErrMessage = message?.ToString();
            Detail = detail?.ToString();
            Cause = cause;
        }
    }
}
