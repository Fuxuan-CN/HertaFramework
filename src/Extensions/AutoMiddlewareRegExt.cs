using NLog;
using Herta.Utils.Logger;
using Herta.Decorators.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace Herta.Extensions.AutoMiddlewareRegExt
{
    public static class ApplicationBuilderExtensions
    {
        private static readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(ApplicationBuilderExtensions));

        // 默认扫描当前程序集
        public static IApplicationBuilder UseAutoMiddleware(this IApplicationBuilder app)
        {
            return UseAutoMiddleware(app, Assembly.GetExecutingAssembly());
        }

        // 允许用户指定要扫描的程序集
        public static IApplicationBuilder UseAutoMiddleware(this IApplicationBuilder app, params Assembly[] assemblies)
        {
            var middlewareTypes = assemblies.SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttributes<MiddlewareAttribute>().Any())
                .OrderBy(t => t.GetCustomAttribute<MiddlewareAttribute>()?.Order ?? int.MaxValue)
                .ToList();

            foreach (var middlewareType in middlewareTypes)
            {
                var middlewareAttribute = middlewareType.GetCustomAttribute<MiddlewareAttribute>();

                _logger.Trace($"Registering middleware {middlewareType.Name} with order {middlewareAttribute!.Order!}");

                app.UseMiddleware(middlewareType!);
            }

            return app;
        }
    }
}