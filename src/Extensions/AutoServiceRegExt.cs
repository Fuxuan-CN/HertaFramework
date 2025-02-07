using System;
using System.Collections.Generic;
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

        // 默认扫描当前程序集
        public static IServiceCollection AutoRegisterServices(this IServiceCollection services)
        {
            return AutoRegisterServices(services, Assembly.GetExecutingAssembly());
        }

        // 允许用户指定要扫描的程序集
        public static IServiceCollection AutoRegisterServices(this IServiceCollection services, params Assembly[] assemblies)
        {
            var serviceTypes = assemblies.SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttributes<ServiceAttribute>().Any())
                .ToList();

            foreach (var type in serviceTypes)
            {
                try
                {
                    var serviceAttribute = type.GetCustomAttribute<ServiceAttribute>();
                    if (serviceAttribute != null)
                    {
                        var interfaces = type.GetInterfaces();
                        foreach (var @interface in interfaces)
                        {
                            _logger.Trace($"Registering service: {@interface.Name} -> {type.Name} (Lifetime: {serviceAttribute.Lifetime})");
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
                catch (Exception ex)
                {
                    _logger.Warn($"Error registering service {type.Name}, maybe not avaliabie in current environment., the error is {ex.GetType().Name}: {ex.Message}");
                }
            }

            return services;
        }
    }
}