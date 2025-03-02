using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Herta.Decorators.Services;
using Herta.Utils.Logger;
using NLog;

namespace Herta.Extensions.AutoServiceRegExt;

public static class ServiceCollectionExtensions
{
    private static readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(ServiceCollectionExtensions));

    public static IServiceCollection AutoRegisterServices(this IServiceCollection services)
    {
        return AutoRegisterServices(services, Assembly.GetExecutingAssembly());
    }

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
                    var interfaces = type.GetInterfaces().ToList();
                    interfaces.AddRange(type.GetInterfaces()
                        .Where(i => i.IsGenericTypeDefinition || i.ContainsGenericParameters)
                        .Select(i => i.GetGenericTypeDefinition())
                        .Distinct());

                    foreach (var @interface in interfaces)
                    {
                        RegisterService(services, @interface, type, serviceAttribute);
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

        _logger.Trace($"{serviceType.Name} <=> {implementationType.Name} registered with lifetime {serviceAttribute.Lifetime}.");
    }
}
