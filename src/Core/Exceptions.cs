
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

    public class WebsocketException : HertaException
    // raise when there is an error with the websocket connection
    {
        public WebsocketException(string message, Exception? innerException = null) : base(message, innerException) {}
    }

    public class WebsocketClosedException : WebsocketException
    // raise when the websocket connection is closed
    {
        public WebsocketClosedException(string message, Exception? innerException = null) : base(message, innerException) {}
    }
}