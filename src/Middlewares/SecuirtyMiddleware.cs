using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Herta.Decorators.Security;
using Herta.Decorators.Middleware;
using Herta.Interfaces.ISecurityPolicy;
using Herta.Utils.Logger;
using Herta.Utils.RouteCacheMatcher;
using NLog;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Herta.Security.MiddlewarePolicy.SpeedLimitSecurityPolicy;

namespace Herta.Middlewares.SecurityMiddleware
{
    [Middleware(Order = 1)]
    public class SecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ISecurityPolicy _defaultSecurityPolicy = new SpeedLimitSecurityPolicy();
        private readonly IActionDescriptorCollectionProvider _actionDescriptorProvider;
        private static readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(SecurityMiddleware));

        public SecurityMiddleware(RequestDelegate next, IActionDescriptorCollectionProvider actionDescriptorProvider)
        {
            _next = next;
            _actionDescriptorProvider = actionDescriptorProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Connection.RemoteIpAddress == null)
            {
                _logger.Warn("The request does not have an IP address.");
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid request: missing IP address.");
                return;
            }

            var ipAddr = context.Connection.RemoteIpAddress.ToString();
            _logger.Trace($"Connection request from {ipAddr}.");

            // 获取请求路径
            var requestPath = context.Request.Path.Value;

            // 查找匹配的控制器和动作描述符
            var actionDescriptor = _actionDescriptorProvider.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .FirstOrDefault(x => IsActionMatch(x, requestPath!));

            var enableSecurity = true;
            ISecurityPolicy policy = _defaultSecurityPolicy;

            if (actionDescriptor != null)
            {
                _logger.Trace($"Found action descriptor for {actionDescriptor.ActionName} in {actionDescriptor.ControllerName}.");
                var methodInfo = actionDescriptor.MethodInfo;
                var controllerType = actionDescriptor.ControllerTypeInfo.AsType();
                var methodAttr = methodInfo.GetCustomAttributes<SecurityProtectAttribute>(false).FirstOrDefault();
                var controllerAttr = controllerType.GetCustomAttributes<SecurityProtectAttribute>(false).FirstOrDefault();

                if (methodAttr != null)
                {
                    enableSecurity = methodAttr.EnableSecurity;
                    policy = methodAttr.PolicyType != null ? 
                             (ISecurityPolicy)context.RequestServices.GetService(methodAttr.PolicyType)! : 
                             _defaultSecurityPolicy;
                    _logger.Trace($"Security is enabled for method {methodInfo.Name} using policy {policy.GetType().Name}.");
                }
                else if (controllerAttr != null)
                {
                    enableSecurity = controllerAttr.EnableSecurity;
                    policy = controllerAttr.PolicyType != null ? 
                             (ISecurityPolicy)context.RequestServices.GetService(controllerAttr.PolicyType)! : 
                             _defaultSecurityPolicy;
                    _logger.Trace($"Security is enabled for controller {controllerType.Name} using policy {policy.GetType().Name}.");
                }
            }
            else
            {
                _logger.Trace($"No action descriptor found for {requestPath}, using default policy.");
            }

            if (!enableSecurity)
            {
                _logger.Trace($"Security is disabled, without checking.");
                await _next(context);
                return;
            }

            _logger.Trace($"Checking access for {ipAddr} using policy {policy.GetType().Name}.");
            var isAllowed = await policy.IsRequestAllowed(ipAddr);
            if (!isAllowed)
            {
                _logger.Trace($"Access denied for {ipAddr}.");
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Access denied due to suspicious activity.");
                return;
            }

            _logger.Trace($"Access granted for {ipAddr}.");
            await _next(context);
        }

        private bool IsActionMatch(ControllerActionDescriptor actionDescriptor, string requestPath)
        {
            // 获取控制器的路由模板
            var controllerRoute = actionDescriptor.AttributeRouteInfo?.Template ?? "";
            // 获取动作的路由模板
            var actionRoute = actionDescriptor.AttributeRouteInfo?.Template ?? actionDescriptor.ActionName;

            // 拼接完整的路由模板
            var fullRouteTemplate = $"/{controllerRoute.Trim('/')}/{actionRoute.Trim('/')}".Replace("//", "/");

            // 使用 RouteCacheMatcher 匹配请求路径和路由模板
            return RouteCacheMatcher.IsPathMatch(requestPath, fullRouteTemplate, out _);
        }
    }
}