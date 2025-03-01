
using System;
using Herta.Exceptions.HertaException;

namespace Herta.Exceptions.WebsocketExceptions.WebsocketException;

public class WebsocketException : HertaBaseException
// raise when there is an error with the websocket connection
{
    public WebsocketException(string message, Exception? innerException = null) : base(message, innerException) { }
}
