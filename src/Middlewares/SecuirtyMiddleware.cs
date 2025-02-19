using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Herta.Decorators.Security;
using Herta.Decorators.Middleware;
using Herta.Interfaces.ISecurityPolicy;
using Herta.Utils.Logger;
using Herta.Utils.MemoryCache;
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
        private readonly MemoryCache<string, ISecurityPolicy> _policyCache = new MemoryCache<string, ISecurityPolicy>();
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

            // 获取当前请求的控制器和动作描述符
            var endpoint = context.GetEndpoint();
            var actionDescriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();

            if (actionDescriptor == null)
            {
                _logger.Debug("No controller action descriptor found, using default policy.");
                await ApplySecurityPolicy(context, _defaultSecurityPolicy);
                return;
            }

            _logger.Debug($"Found action descriptor for {actionDescriptor.ActionName} in {actionDescriptor.ControllerName}.");

            // 获取控制器和动作上的安全策略属性
            var methodInfo = actionDescriptor.MethodInfo;
            var controllerType = actionDescriptor.ControllerTypeInfo.AsType();
            var methodAttr = methodInfo.GetCustomAttributes<SecurityProtectAttribute>(false).FirstOrDefault();
            var controllerAttr = controllerType.GetCustomAttributes<SecurityProtectAttribute>(false).FirstOrDefault();

            // 确定最终的安全策略
            var policy = GetOrCachePolicy(actionDescriptor.ControllerName, actionDescriptor.ActionName, ipAddr, methodAttr, controllerAttr);

            // 应用安全策略
            await ApplySecurityPolicy(context, policy);
        }

        private ISecurityPolicy GetOrCachePolicy(string controllerName, string actionName, string ipAddr, SecurityProtectAttribute? methodAttr, SecurityProtectAttribute? controllerAttr)
        {
            // 构造缓存键 (类似于Scoped，避免多个连接共享同一个策略实例，导致并发问题)
            // 比如： 当A和B连接共享一个令牌桶，A大量请求，占用大量令牌，导致B得不到令牌，B的请求被拒绝
            // 通过控制器名字-动作方法-ip地址的缓存键可以确保每个连接都有自己的策略实例，解决了并发问题
            var cacheKey = $"{controllerName}:{actionName}:{ipAddr}";

            // 从缓存中获取策略实例
            if (_policyCache.TryGet(cacheKey, out var policy))
            {
                return policy;
            }

            // 如果没有缓存，根据属性创建策略实例
            policy = CreatePolicyInstance(methodAttr ?? controllerAttr);

            // 将策略实例添加到缓存中
            // 优化，确保不是default策略，才缓存
            if (policy != _defaultSecurityPolicy)
            {
                _policyCache.Set(cacheKey, policy, TimeSpan.FromMinutes(10));
            }
            return policy;
        }

        private ISecurityPolicy CreatePolicyInstance(SecurityProtectAttribute? attr)
        {
            if (attr == null || attr.PolicyType == null)
            {
                return _defaultSecurityPolicy;
            }

            try
            {
                var policyInstance = Activator.CreateInstance(attr.PolicyType) as ISecurityPolicy;
                if (policyInstance == null)
                {
                    throw new InvalidOperationException($"Failed to create instance of {attr.PolicyType.Name}");
                }
                return policyInstance;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, $"Error creating policy instance for {attr.PolicyType.Name}: {ex.Message}, did you have a no-arg constructor?");
                return _defaultSecurityPolicy;
            }
        }

        private async Task ApplySecurityPolicy(HttpContext context, ISecurityPolicy policy)
        {
            _logger.Debug($"Checking access for {context.Connection.RemoteIpAddress} using policy {policy.GetType().Name}.");

            var isAllowed = await policy.IsRequestAllowed(context);
            if (!isAllowed)
            {
                _logger.Debug($"Access denied for {context.Connection.RemoteIpAddress}.");
                context.Response.StatusCode = await policy.GetStatusCode();
                var reason = await policy.GetBlockedReason();
                await context.Response.WriteAsync(reason ?? "Access denied.");
                return;
            }

            _logger.Debug($"Access granted for {context.Connection.RemoteIpAddress}.");
            await _next(context);
        }
    }
}