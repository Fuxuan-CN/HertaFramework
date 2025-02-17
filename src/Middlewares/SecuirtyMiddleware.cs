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
using Herta.Security.MiddlewarePolicy.ExampleSecurityPolicy;

namespace Herta.Middlewares.SecurityMiddleware
{
    [Middleware(Order = 1)]
    public class SecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ISecurityPolicy _defaultSecurityPolicy = new ExampleSecurityPolicy();
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
                _logger.Debug($"Found action descriptor for {actionDescriptor.ActionName} in {actionDescriptor.ControllerName}.");
                var methodInfo = actionDescriptor.MethodInfo;
                var controllerType = actionDescriptor.ControllerTypeInfo.AsType();
                var methodAttr = methodInfo.GetCustomAttributes<SecurityProtectAttribute>(false).FirstOrDefault();
                var controllerAttr = controllerType.GetCustomAttributes<SecurityProtectAttribute>(false).FirstOrDefault();

                if (methodAttr != null)
                {
                    _logger.Trace("method attr found, checking security.");
                    enableSecurity = methodAttr.EnableSecurity;
                    policy = MakePolicyHandler(methodAttr.PolicyType);
                }
                else if (controllerAttr != null)
                {
                    _logger.Trace("controller attr found, checking security.");
                    enableSecurity = controllerAttr.EnableSecurity;
                    policy = MakePolicyHandler(controllerAttr.PolicyType);
                }
            }
            else
            {
                _logger.Debug($"No action descriptor found for {requestPath}, using default policy.");
            }

            if (!enableSecurity)
            {
                _logger.Debug($"Security is disabled, without checking.");
                await _next(context);
                return;
            }

            _logger.Trace($"Checking access for {ipAddr} using policy {policy.GetType().Name}.");
            var isAllowed = await policy.IsRequestAllowed(context);
            if (!isAllowed)
            {
                _logger.Debug($"Access denied for {ipAddr}.");
                context.Response.StatusCode = await policy.GetStatusCode();
                var reason = await policy.GetBlockedReason();
                await context.Response.WriteAsync(reason ?? "Access denied.");
                return;
            }

            _logger.Debug($"Access granted for {ipAddr}.");
            await _next(context);
        }

        private ISecurityPolicy MakePolicyHandler(Type PolicyType)
        {
            // 尝试获取无参构造函数
            var constructorInfo = PolicyType.GetConstructor(Type.EmptyTypes);
            ISecurityPolicy? _policy = null;

            if (constructorInfo != null)
            {
                // 如果存在无参构造函数，则使用它来创建实例
                _policy = (ISecurityPolicy)constructorInfo.Invoke(null);
            }
            else
            {
                // 没有无参构造函数，尝试获取有参构造函数
                constructorInfo = PolicyType.GetConstructors().FirstOrDefault();
                if (constructorInfo != null)
                {
                    // 获取有参构造函数的参数类型和默认值
                    var parameters = constructorInfo.GetParameters();
                    var parameterValues = new object[parameters.Length];

                    foreach (var parameter in parameters)
                    {
                        parameterValues[parameter.Position] = parameter.DefaultValue ?? new object();
                    }

                    // 使用有参构造函数创建实例
                    _policy = (ISecurityPolicy)constructorInfo.Invoke(parameterValues);
                }
            }

            // 如果无法创建实例，则使用默认策略
            return _policy ?? _defaultSecurityPolicy;
        }

        private bool IsActionMatch(ControllerActionDescriptor actionDescriptor, string requestPath)
        {
            // 获取动作的路由模板
            var actionRoute = actionDescriptor.AttributeRouteInfo?.Template ?? actionDescriptor.ActionName;

            // 拼接完整的路由模板
            var fullRouteTemplate = $"/{actionRoute.Trim('/')}".Replace("//", "/");
            _logger.Trace($"matching {requestPath} with {fullRouteTemplate}.");
            // 使用 RouteCacheMatcher 匹配请求路径和路由模板
            return RouteCacheMatcher.IsPathMatch(requestPath, fullRouteTemplate, out _);
        }
    }
}