using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Herta.Decorators.Services;
using Herta.Utils.Logger;

namespace Herta.Extensions.AutoServiceRegExt
{
    public static class ServiceCollectionExtensions
    {
        private static readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(ServiceCollectionExtensions));

        public static IServiceCollection AutoRegisterServices(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly(); // 获取当前程序集
            var types = assembly.GetTypes(); // 获取程序集中所有类型

            foreach (var type in types)
            {
                var serviceAttribute = type.GetCustomAttribute<ServiceAttribute>(); // 查找ServiceAttribute特性
                if (serviceAttribute != null)
                {
                    // 获取所有接口
                    var interfaces = type.GetInterfaces();
                    foreach (var @interface in interfaces)
                    {
                        _logger.Trace($"Registering service: {@interface.Name} -> {type.Name} (Lifetime: {serviceAttribute.Lifetime})");
                        // 根据生命周期注册服务
                        switch (serviceAttribute.Lifetime)
                        {
                            case ServiceLifetime.Scoped:
                                services.AddScoped(@interface, type);
                                break;
                            case ServiceLifetime.Singleton:
                                services.AddSingleton(@interface, type);
                                break;
                            case ServiceLifetime.Transient:
                                services.AddTransient(@interface, type);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(serviceAttribute.Lifetime), serviceAttribute.Lifetime, null);
                        }
                    }
                }
            }

            return services;
        }
    }
}