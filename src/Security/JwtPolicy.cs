using Herta.Exceptions.HttpException;
using Herta.Decorators.AuthorizeRegister;
using Herta.Security.Requirements.JwtRequire;
using Herta.Interfaces.IAuthService;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

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

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, JwtRequirement requirement)
        {
            var httpContext = context.Resource as HttpContext;
            var authHeaderStr = httpContext!.Request.Headers["Authorization"]!.ToString();
            if (string.IsNullOrEmpty(authHeaderStr))
            {
                throw new HttpException(StatusCodes.Status401Unauthorized, "无法授权，请提供有效的token，请求头Authorization: Bearer <token>缺失");
            }

            var token = authHeaderStr.Split("Bearer ")[1];

            if (string.IsNullOrEmpty(token))
            {
                throw new HttpException(StatusCodes.Status401Unauthorized, "无效的token");
            }

            if (!await _authService.AuthorizeAsync(token))
            {
                throw new HttpException(StatusCodes.Status401Unauthorized, "无效的token");
            }

            context.Succeed(requirement);
        }
    }
}