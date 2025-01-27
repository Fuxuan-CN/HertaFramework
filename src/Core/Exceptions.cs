
using System;

namespace Herta.Exceptions
{
    public class HertaException : Exception
    // base class for all HertaApi exceptions
    {
        public HertaException(string message, Exception? innerException) : base(message, innerException) {}
    }

    public class HttpException : HertaException
    // raise when there is an error with the http request or response
    {
        public int StatusCode { get; }
        public string? Detail { get; }
        public Exception? Cause { get; }
        public HttpException(int statusCode, object? message, Exception? cause = null)
            : base(message?.ToString() ?? "Unknown error", cause)
        {
            StatusCode = statusCode;
            Detail = message?.ToString();
            Cause = cause;
        }
    }
}