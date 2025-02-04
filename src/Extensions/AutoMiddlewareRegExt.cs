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

        public static IApplicationBuilder UseAutoMiddleware(this IApplicationBuilder app)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies(); // 获取所有作用域的程序集

            var middlewareTypes = assemblies.SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttributes<MiddlewareAttribute>().Any())
                .OrderBy(t => t.GetCustomAttribute<MiddlewareAttribute>()?.Order ?? int.MaxValue)
                .ToList();

            foreach (var middlewareType in middlewareTypes)
            {
                _logger.Trace($"Register middleware {middlewareType.Name}");
                app.UseMiddleware(middlewareType);
            }

            return app;
        }
    }
}