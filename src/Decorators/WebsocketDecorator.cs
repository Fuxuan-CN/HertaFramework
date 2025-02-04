using System;

namespace Herta.Decorators.Websocket
{
    // 标记一个控制器方法为 websocket 路由，指定路径，再请求的时候会用 websocket 协议进行通信
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
