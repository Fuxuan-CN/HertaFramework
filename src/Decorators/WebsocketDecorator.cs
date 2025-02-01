using System;

namespace Herta.Decorators.Websocket
{
    [AttributeUsage(AttributeTargets.Method)]
    public class WebsocketAttribute : Attribute
    {
        public string Path { get; }

        public WebsocketAttribute(string? path = null)
        {
            Path = path ?? string.Empty;
        }
    }
}