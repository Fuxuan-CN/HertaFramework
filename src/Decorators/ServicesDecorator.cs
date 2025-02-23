
using System;
using Microsoft.Extensions.DependencyInjection;

namespace Herta.Decorators.Services
{
    // 标记一个类为服务，启动的时候会自动注册到DI容器中
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ServiceAttribute : Attribute
    {
        public ServiceLifetime Lifetime { get; }
        public ServiceAttribute(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            Lifetime = lifetime;
        }
    }
}
