using System;

namespace Herta.Decorators.Middleware;

// 标记一个中间件，在启动的时候会自动加载
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MiddlewareAttribute : Attribute
{
    public int Order { get; set; } = 0;
    public bool Enabled { get; set; } = true;
}
