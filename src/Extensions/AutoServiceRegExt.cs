using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Herta.Decorators.Services;
using Herta.Utils.Logger;
using NLog;

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
                        // 注册普通接口
                        var interfaces = type.GetInterfaces();
                        foreach (var @interface in interfaces)
                        {
                            _logger.Trace($"Registering service: {@interface.Name} -> {type.Name} (Lifetime: {serviceAttribute.Lifetime})");
                            RegisterService(services, @interface, type, serviceAttribute);
                        }

                        // 处理泛型接口
                        var genericInterfaces = type.GetInterfaces()
                            .Where(i => i.IsGenericTypeDefinition || i.ContainsGenericParameters)
                            .Select(i => i.GetGenericTypeDefinition())
                            .Distinct();

                        foreach (var genericInterface in genericInterfaces)
                        {
                            var concreteType = type;
                            var interfaceType = genericInterface.MakeGenericType(concreteType.GetGenericArguments());
                            _logger.Trace($"Registering generic service: {interfaceType.Name} -> {concreteType.Name} (Lifetime: {serviceAttribute.Lifetime})");
                            RegisterService(services, interfaceType, concreteType, serviceAttribute);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Error registering service {type.Name}, maybe not available in current environment., the error is {ex.GetType().Name}: {ex.Message}");
                }
            }

            return services;
        }

        private static void RegisterService(IServiceCollection services, Type serviceType, Type implementationType, ServiceAttribute serviceAttribute)
        {
            switch (serviceAttribute.Lifetime)
            {
                case ServiceLifetime.Scoped:
                    services.AddScoped(serviceType, implementationType);
                    break;
                case ServiceLifetime.Singleton:
                    services.AddSingleton(serviceType, implementationType);
                    break;
                case ServiceLifetime.Transient:
                    services.AddTransient(serviceType, implementationType);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(serviceAttribute.Lifetime), serviceAttribute.Lifetime, null);
            }
        }
    }
}
