
using System;
using Herta.Exceptions.WebsocketExceptions.WebsocketException;

namespace Herta.Exceptions.WebsocketExceprions.WebsocketClosedException;

public sealed class WebsocketClosedException : WebsocketException
// raise when there is an error with the websocket connection
{
    public WebsocketClosedException(string message, Exception? innerException = null) : base(message, innerException) { }
}
