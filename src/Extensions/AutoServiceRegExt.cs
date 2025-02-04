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
            var assemblies = AppDomain.CurrentDomain.GetAssemblies(); // 获取所有作用域的程序集
            var serviceTypes = assemblies.SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttributes<ServiceAttribute>().Any())
                .ToList();

            foreach (var type in serviceTypes)
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