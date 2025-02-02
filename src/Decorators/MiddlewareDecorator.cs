using System;

namespace Herta.Decorators.Middleware
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MiddlewareAttribute : Attribute
    {
        public int Order { get; set; } = 0;
    }
}