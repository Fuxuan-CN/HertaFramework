
# Herta Framework

## Introduction

Herta is an open-source framework based on ASP.NET Core, designed to simplify the development of Web APIs and WebSockets. By automatically discovering and registering services, handling global exceptions, implementing custom response mechanisms, and simplifying WebSocket development, Herta offers a highly efficient, flexible, and user-friendly development environment.

## Features

- **Automatic Service Discovery and Registration:**
  - Use the `[Service]` attribute to mark service classes and automatically register them in the dependency injection container.
    - Supports various lifetimes (Scoped, Singleton, Transient).

- **Global Exception Handling:**
  - Capture and handle all exceptions through `GlobalExceptionMiddleware`.
    - Support custom `HttpException` to return specified HTTP status codes and error messages.

- **Custom Response Mechanism:**
  - Provides `Response` class and its subclasses (e.g., `JsonResponse`, `TextResponse`, `FileResponse`, etc.) to simplify response construction.
    - Supports a unified error response format.

- **Simplified WebSocket Development:**
  - Use the `[Websocket]` attribute to mark controller methods and automatically handle WebSocket requests.
    - Provides `WebSocketMiddleware` to simplify WebSocket development.

- **Automatic Middleware Registration:**
  - Use the `[Middleware]` attribute to mark middleware classes and automatically register them in the middleware pipeline.
    - Support specifying the loading order of middleware.

### Installation Guide

- Manual Installation

```bash
git clone https://github.com/Fuxuan-CN/HertaFramework
```

### Quick Start

- You only need to:

```csharp
using Herta.Core.Server;

var server = new HertaApiServer(debug: true, IgnoreConfigureWarning: true);
server.Run();

```

- Creating a Service
Create a service class and mark it with the `[Service]` attribute:

```csharp
using Herta.Decorators.Services;
using Microsoft.Extensions.DependencyInjection;
using YourNamespace.Interfaces.YourIService;

namespace MyHertaProject.Services
{
    [Service(ServiceLifetime.Scoped)]
    public class MyService : YourIService
    {
        public string GetMessage()
        {
            return "Hello, World!";
        }
    }
}
```

- Creating a Controller

Create a controller and use Herta's response mechanism:

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

- Running the Project
Run the project and visit the URL configured in your configuration file. You should see the returned JSON response:

```json
{
    "Message": "Hello, World!"
}
```

### Feature Overview

- **Automatic Service Discovery and Registration**

```csharp
// namespace MyProject.Services.MyService
using Herta.Decorators.Services;
using Microsoft.Extensions.DependencyInjection;
using YourNamespace.Interfaces.YourIService;

[Service(ServiceLifetime.Scoped)]
public class MyService : YourIService
{
    public string GetMessage()
    {
        return "Hello, World!";
    }
}
```

- **Global Exception Handling**

Capture and handle all exceptions through `GlobalExceptionMiddleware`. You can throw an `HttpException` to return custom HTTP status codes and error messages:

```csharp
catch (Exception ex)
{
    throw new HttpException(400, "Invalid request", ex);
}
```

- **Custom Response Mechanism**

Herta provides various response classes such as `JsonResponse`, `TextResponse`, `FileResponse`, etc., to simplify response construction:

```csharp
return new JsonResponse(new { Message = "Hello, World!" });
```

- **Simplified WebSocket Development**
Mark controller methods with the `[Websocket]` attribute to automatically handle WebSocket requests:

```csharp
using Herta.Decorators.Websocket; // WebSocket decorator
using Herta.Utils.HertaWebsocket; // WebSocket manager

[Websocket("ws/echo")]
public async Task Echo(HertaWebsocket webSocket)
{
    // WebSocket handling logic
}
```

- **Automatic Middleware Registration**
Mark middleware classes with the `[Middleware]` attribute to automatically register them in the middleware pipeline:

```csharp
using Herta.Decorators.Middleware;
using Microsoft.AspNetCore.Http;

[Middleware(Order = 1)]
public class MyMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Middleware logic
    }
}
```

- **Authorization Policy Registration:**
When using JwtBearer authorization policy, you can mark controller methods with the `[AuthHandler]` attribute and specify the authorization policy name:

Below is an implementation of the JwtBearer authorization policy:

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

### Configuration Options

- **Service Lifetime**

You can specify the service lifetime using the `[Service]` attribute:

```csharp
[Service(ServiceLifetime.Singleton)]
public class MySingletonService
{
    // Service implementation
}
```

- **Middleware Order**
You can specify the loading order of middleware using the `[Middleware]` attribute:

```csharp
[Middleware(Order = 2)]
public class MySecondMiddleware
{
    // Middleware logic
}
```

### Contribution Guidelines

Contributions are welcome! Please follow these guidelines:

- **Code Standards:** Follow C# coding standards to ensure the code is clear and readable.
- **Submit PR:** Ensure the code passes all tests before submitting a PR.
- **Documentation Updates:** Update relevant documentation if new features are added.
- **License**
Herta Framework is licensed under the MIT License. You are free to use, modify, and distribute the code, but must retain the license statement.
