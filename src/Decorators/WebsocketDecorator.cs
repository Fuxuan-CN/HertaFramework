using System;

namespace Herta.Decorators.Websocket
{
    // 标记一个控制器方法为 websocket 路由，指定路径，再请求的时候会用 websocket 协议进行通信
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class WebsocketAttribute : Attribute
    {
        public string Path { get; }
        public List<string> Parameters { get; } = new List<string>();

        public WebsocketAttribute(string? path = null)
        {
            Path = path ?? string.Empty;
            // 解析路径中的动态参数
            if (!string.IsNullOrEmpty(Path))
            {
                var segments = Path.Split('/');
                foreach (var segment in segments)
                {
                    if (segment.StartsWith("{") && segment.EndsWith("}"))
                    {
                        Parameters.Add(segment.Trim('{', '}'));
                    }
                }
            }
        }
    }
}
