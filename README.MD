
# Herta Framework

## 介绍

Herta 是一个基于 ASP.NET Core 的开源框架，旨在简化 Web API 和 WebSocket 的开发。通过自动发现和注册服务、全局异常处理、自定义响应机制以及 WebSocket 的简化开发，Herta 提供了一个高效、灵活且易于使用的开发环境。

## 功能特点

- 自动发现和注册服务：
    使用 [Service] 特性标记服务类，自动注册到依赖注入容器。
    支持多种生命周期（Scoped、Singleton、Transient）。

- 全局异常处理：
    通过 GlobalExceptionMiddleware 捕获并处理所有异常。
    支持自定义 HttpException，返回指定的 HTTP 状态码和错误信息。

- 自定义响应机制：
    提供 Response 类及其子类（如 JsonResponse、TextResponse、FileResponse 等），简化响应构建。
    支持统一的错误响应格式。

- WebSocket 简化开发：
    使用 [Websocket] 特性标记控制器方法，自动处理 WebSocket 请求。
    提供 WebSocketMiddleware，简化 WebSocket 的开发。

- 中间件自动注册：
    使用 [Middleware] 特性标记中间件类，自动注册到中间件管道。
    支持指定中间件的加载顺序。

### 安装指南

- 手动安装

```bash
git clone https://github.com/Fuxuan-CN/HertaFramework
```

### 快速开始

- 你只需要:

```csharp
using Herta.Core.Server;

public class Program
{
    public static void Main()
    {
        var server = new HertaApiServer(debug: true, needAuthentication: true);
        server.Run();
    }
}
```

- 创建一个服务
创建一个服务类并使用 [Service] 特性标记：

```csharp
using Herta.Decorators.Services;

namespace MyHertaProject.Services
{
    [Service(ServiceLifetime.Scoped)]
    public class MyService
    {
        public string GetMessage()
        {
            return "Hello, World!";
        }
    }
}

```

- 创建一个控制器

创建一个控制器并使用 Herta 的响应机制：

```csharp
using Microsoft.AspNetCore.Mvc;
using Herta.Responses.Response;
using MyProject.Services.MyService;

namespace MyProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MyController : ControllerBase
    {
        private readonly MyService _myService;

        public MyController(MyService myService)
        {
            _myService = myService;
        }

        [HttpGet]
        public JsonResponse Get()
        {
            var message = _myService.GetMessage();
            return new JsonResponse(new { Message = message });
        }
    }
}
```

- 运行项目
运行项目并访问你配置文件中配置的 URL，你应该会看到返回的 JSON 响应：

```json
{
    "Message": "Hello, World!"
}
```

### 功能介绍

- 自动发现和注册服务

```csharp
// namespace MyProject.Services.MyService
using Herta.Decorators.Services;
using Microsoft.Extensions.DependencyInjection;

[Service(ServiceLifetime.Scoped)]
public class MyService
{
    public string GetMessage()
    {
        return "Hello, World!";
    }
}
```

- 全局异常处理

通过 GlobalExceptionMiddleware 捕获并处理所有异常。你可以抛出 HttpException 来返回自定义的 HTTP 状态码和错误信息：

```csharp
catch (Exception ex)
{
    throw new HttpException(400, "Invalid request", ex);
}
```

- 自定义响应机制

Herta 提供了多种响应类，如 JsonResponse、TextResponse、FileResponse 等，简化响应构建：

```csharp
return new JsonResponse(new { Message = "Hello, World!" });
```

- WebSocket 简化开发
通过 [Websocket] 特性标记控制器方法，自动处理 WebSocket 请求：

```csharp
using Herta.Decorators.Websocket; // WebSocket 装饰器
using Herta.Utils.HertaWebsocket; // WebSocket 管理器

[Websocket("ws/echo")]
public async Task Echo(HertaWebsocket webSocket)
{
    // WebSocket 处理逻辑
}
```

- 中间件自动注册
通过 [Middleware] 特性标记中间件类，自动注册到中间件管道：

```csharp
using Herta.Decorators.Middleware;

[Middleware(Order = 1)]
public class MyMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        // 中间件逻辑
    }
}
```

- 授权策略注册：
当你用 JwtBearer 授权策略时，你可以通过 [AuthHandler] 特性标记控制器方法，并指定授权策略名称：

下面是一个 JwtBearer 授权策略的实现：

```csharp
using Herta.Exceptions.HttpException;
using Herta.Decorators.AuthRegDecorator;
using Herta.Security.Requirements.JwtRequire;
using Herta.Interfaces.IAuthService;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

namespace Herta.Security.Authorization
{
    [AuthHandler(typeof(JwtRequirement), "JwtAuth")]
    public class JwtAuthorizationHandler : AuthorizationHandler<JwtRequirement>
    {
        private readonly IAuthService _authService;

        public JwtAuthorizationHandler(IAuthService authService)
        {
            _authService = authService;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, JwtRequirement requirement)
        {
            var httpContext = context.Resource as HttpContext;
            var authHeader = httpContext!.Request.Headers["Authorization"]!;
            var authHeaderStr = authHeader.FirstOrDefault();
            if (string.IsNullOrEmpty(authHeaderStr))
            {
                throw new HttpException(StatusCodes.Status401Unauthorized, "Authorization header not found");
            }
            var token = Regex.Match(authHeaderStr!, "Bearer (.*)").Groups[1].Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new HttpException(StatusCodes.Status401Unauthorized, "Invalid token format");
            }

            if (!_authService.AuthorizeAsync(token).GetAwaiter().GetResult())
            {
                throw new HttpException(StatusCodes.Status401Unauthorized, "Invalid token");
            }

            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}
```

### 配置选项

- 服务生命周期

你可以通过 [Service] 特性指定服务的生命周期：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Herta.Decorators.Services;
using YourNamespace.YourInterface.YourIinterfaceClass;

[Service(ServiceLifetime.Singleton)]
public class MySingletonService : IYourInterface
{
    // 服务实现
}
```

- 中间件顺序
你可以通过 [Middleware] 特性指定中间件的加载顺序：

```csharp
using Herta.Decorators.Middleware;

[Middleware(Order = 2)]
public class MySecondMiddleware
{
    // 中间件逻辑
}
```

### 贡献指南

欢迎贡献代码！请遵循以下指南：

- 代码规范：遵循 C# 编程规范，确保代码清晰易读。
- 提交 PR：在提交 PR 之前，请确保代码通过所有测试。
- 文档更新：如果添加了新功能，请更新相关文档。
- 许可证：
Herta Framework 采用 MIT License 许可证。你可以自由使用、修改和分发代码，但需保留许可证声明。
