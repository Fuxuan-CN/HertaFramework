
using Herta.Middleware.Websocket;

namespace Herta.Extensions.WebsocketExt
{
    public static class WebSocketMiddlewareExtensions
    {
        public static IApplicationBuilder UseHertaWebSockets(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<WebSocketMiddleware>();
        }
    }
}