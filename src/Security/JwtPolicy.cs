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
        string token = "";
        var authHeaderStr = httpContext!.Request.Headers["Authorization"]!.ToString();
        if (string.IsNullOrEmpty(authHeaderStr))
        {
            context.Fail(Reason("Authorization header is missing"));
        }
        try
        {
            token = authHeaderStr.Split("Bearer ")[1];
        }
        catch (Exception)
        {
            context.Fail(Reason("Authorization format is invalid"));
        }

        if (string.IsNullOrEmpty(token))
        {
            context.Fail(Reason("token is missing"));
        }

        if (!await _authService.AuthorizeAsync(token))
        {
            context.Fail(Reason("Invalid token"));
        }

        context.Succeed(requirement);
    }
}
