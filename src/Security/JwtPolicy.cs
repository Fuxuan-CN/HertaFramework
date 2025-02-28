using Herta.Exceptions.HttpException;
using Herta.Decorators.AuthorizeRegister;
using Herta.Security.Requirements.JwtRequire;
using Herta.Interfaces.IAuthService;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Herta.Security.Authorization;

[AuthHandler(typeof(JwtRequirement), "JwtAuth")]
public class JwtAuthorizationHandler : AuthorizationHandler<JwtRequirement>
{
    private readonly IAuthService _authService;

    public JwtAuthorizationHandler(IAuthService authService)
    {
        _authService = authService;
    }

    private AuthorizationFailureReason Reason(string message)
    {
        return new AuthorizationFailureReason(this, message);
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, JwtRequirement requirement)
    {
        var httpContext = context.Resource as HttpContext;
        var authHeaderStr = httpContext!.Request.Headers["Authorization"]!.ToString();
        if (string.IsNullOrEmpty(authHeaderStr))
        {
            context.Fail(Reason("Authorization 请求头缺失"));
        }

        var token = authHeaderStr.Split("Bearer ")[1];

        if (string.IsNullOrEmpty(token))
        {
            context.Fail(Reason("token为空"));
        }

        if (!await _authService.AuthorizeAsync(token))
        {
            context.Fail(Reason("无效的token或token已过期"));
        }

        context.Succeed(requirement);
    }
}
