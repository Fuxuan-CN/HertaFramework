using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Herta.Utils.Logger;
using NLog;
using Herta.Decorators.Websocket;
using Herta.Utils.HertaWebsocket;
using System.Text.RegularExpressions;
using Herta.Decorators.Middleware;

namespace Herta.Middleware.Websocket
{
    /*
    Websocket 中间件
    用于查找标记了 [Websocket] decorator 的控制器方法，并根据请求路径匹配控制器方法，创建 WebSocketManager 并调用控制器方法
    否则, 继续处理请求
    */
    [Middleware(Order = 1)]
    public sealed class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
        private readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(WebSocketMiddleware));
        private static readonly ConcurrentDictionary<string, Regex> RegexCache = new ConcurrentDictionary<string, Regex>();

        public WebSocketMiddleware(RequestDelegate next, IActionDescriptorCollectionProvider actionDescriptorCollectionProvider)
        {
            _next = next;
            _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                _logger.Trace("Websocket request detected.");
                var actionDescriptors = _actionDescriptorCollectionProvider.ActionDescriptors.Items;
                var requestPath = context.Request.Path.Value;
                _logger.Trace($"Websocket request path: {requestPath}");
                _logger.Trace($"Connection from {context.Connection.RemoteIpAddress}:{context.Connection.RemotePort}");

                foreach (var actionDescriptor in actionDescriptors)
                {
                    if (actionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
                    {
                        var methodInfo = controllerActionDescriptor.MethodInfo;
                        var websocketAttribute = methodInfo.GetCustomAttribute<WebsocketAttribute>();
                        if (websocketAttribute != null)
                        {
                            // 获取控制器的根路径
                            var controllerRoute = controllerActionDescriptor.AttributeRouteInfo?.Template ?? "";
                            // 获取WebSocket路径
                            var websocketPath = websocketAttribute.Path;
                            // 拼接完整的WebSocket路径
                            var fullWebsocketPath = $"/{controllerRoute.Trim('/')}/{websocketPath.Trim('/')}".Replace("//", "/");

                            // 解析参数化路径
                            var regex = GetRegex(fullWebsocketPath);
                            var match = regex.Match(requestPath ?? "");

                            if (match.Success)
                            {
                                var parameters = new Dictionary<string, string?>();
                                foreach (var groupName in regex.GetGroupNames())
                                {
                                    if (int.TryParse(groupName, out int groupIndex) == false)
                                    {
                                        parameters[groupName] = match.Groups[groupName].Value ?? null;
                                    }
                                }

                                _logger.Trace($"Matched WebSocket path: {fullWebsocketPath} with parameters: {string.Join(", ", parameters)}");

                                var controllerType = controllerActionDescriptor.ControllerTypeInfo.AsType();
                                var controller = context.RequestServices.GetService(controllerType);

                                // 创建 HertaWebsocket
                                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                                var HertaWs = new HertaWebsocket(webSocket, parameters);

                                // 创建委托
                                var delegateType = typeof(Func<HertaWebsocket, Task>);
                                var methodDelegate = methodInfo.CreateDelegate(delegateType, controller);

                                // 调用控制器方法
                                await ((Func<HertaWebsocket, Task>)methodDelegate)(HertaWs);
                                return;
                            }
                            else
                            {
                                _logger.Trace($"Request path {requestPath} does not match {fullWebsocketPath}. Skipping...");
                            }
                        }
                    }
                }
            }

            await _next(context); // 非WebSocket请求，继续处理
        }

        private string RegexPatternFromTemplate(string template)
        {
            return Regex.Replace(template, @"\{(\w+)\}", "(?<$1>[^/]+)");
        }

        private Regex GetRegex(string template)
        {
            return RegexCache.GetOrAdd(template, new Regex(RegexPatternFromTemplate(template)));
        }
    }
}