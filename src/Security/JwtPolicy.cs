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

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, JwtRequirement requirement)
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

            if (!await _authService.AuthorizeAsync(token))
            {
                throw new HttpException(StatusCodes.Status401Unauthorized, "Invalid token");
            }

            context.Succeed(requirement);
        }
    }
}