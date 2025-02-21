using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Herta.Utils.Logger;
using NLog;
using Herta.Decorators.Websocket;
using Herta.Utils.HertaWebsocketUtil;
using Herta.Decorators.Middleware;
using Herta.Utils.RouteCacheMatcher;

namespace Herta.Middleware.Websocket
{
    [Middleware(Order = 1)]
    public sealed class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
        private readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(WebSocketMiddleware));

        public WebSocketMiddleware(RequestDelegate next, IActionDescriptorCollectionProvider actionDescriptorCollectionProvider)
        {
            _next = next;
            _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                _logger.Debug("Websocket request detected.");
                var requestPath = context.Request.Path.Value;

                // 筛选出带有 WebsocketAttribute 的控制器方法
                var websocketActions = _actionDescriptorCollectionProvider.ActionDescriptors.Items
                    .OfType<ControllerActionDescriptor>()
                    .Select(descriptor => new
                    {
                        Descriptor = descriptor,
                        WebsocketAttribute = descriptor.MethodInfo.GetCustomAttribute<WebsocketAttribute>()
                    })
                    .Where(x => x.WebsocketAttribute != null);

                foreach (var item in websocketActions)
                {
                    var controllerRoute = item.Descriptor.AttributeRouteInfo?.Template ?? "";
                    var websocketPath = item.WebsocketAttribute!.Path!;
                    var fullWebsocketPath = $"/{controllerRoute.Trim('/')}/{websocketPath.Trim('/')}".Replace("//", "/");

                    // 提取路径参数和查询字符串参数
                    var parameters = new Dictionary<string, string?>();

                    // 提取路径参数
                    var pathSegments = requestPath!.Trim('/').Split('/');
                    var templateSegments = fullWebsocketPath.Trim('/').Split('/');
                    if (pathSegments.Length != templateSegments.Length)
                    {
                        continue; // 路径段数不匹配，跳过
                    }

                    for (int i = 0; i < pathSegments.Length; i++)
                    {
                        if (templateSegments[i].StartsWith("{") && templateSegments[i].EndsWith("}"))
                        {
                            var paramName = templateSegments[i].Trim('{', '}');
                            parameters[paramName] = pathSegments[i];
                        }
                        else if (pathSegments[i] != templateSegments[i])
        
                        {
                            continue; // 静态路径部分不匹配，跳过
                        }
                    }

                    // 提取查询字符串参数
                    foreach (var queryParam in context.Request.Query)
                    {
                        parameters[queryParam.Key] = queryParam.Value.FirstOrDefault();
                    }

                    // 检查是否所有动态参数都已提取
                    if (item.WebsocketAttribute.Parameters.All(param => parameters.ContainsKey(param)))
                    {
                        _logger.Trace($"Matched WebSocket path: {fullWebsocketPath} with parameters: {string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"))}");

                        var controllerType = item.Descriptor.ControllerTypeInfo.AsType();
                        var controller = context.RequestServices.GetService(controllerType);

                        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        var HertaWs = new HertaWebsocket(webSocket!, parameters);

                        var delegateType = typeof(Func<HertaWebsocket, Task>);
                        var methodDelegate = item.Descriptor.MethodInfo.CreateDelegate(delegateType, controller);

                        await ((Func<HertaWebsocket, Task>)methodDelegate)(HertaWs);
                        return; // 匹配成功后直接退出方法
                    }
                }
            }

            await _next(context); // 非WebSocket请求或未匹配到，继续处理
        }
    }
}