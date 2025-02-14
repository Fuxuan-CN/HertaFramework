using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Herta.Utils.Logger;
using System.Net;
using NLog;
using System.Threading.Tasks;
using Herta.Exceptions.HttpException;
using Herta.Decorators.Middleware;
using Herta.Interfaces.ISecurityPolicy;

namespace Herta.Middlewares.SecurityMiddleware
{
    [Middleware(Order = 1)]
    public class SecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ISecurityPolicy _securityPolicy;
        private static readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(SecurityMiddleware));

        public SecurityMiddleware(RequestDelegate next, ISecurityPolicy securityPolicy)
        {
            _next = next;
            _securityPolicy = securityPolicy;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var ipAddr = context!.Connection.RemoteIpAddress!.ToString()!;
            _logger.Trace($"Connection request from {ipAddr}");
            bool isAllowed = await _securityPolicy.IsRequestAllowed(ipAddr);
            if (!isAllowed)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                _logger.Warn($"blocked request from {ipAddr}");
                await context.Response.WriteAsync("Access denied due to suspicious activity.");
                return;
            }
            _logger.Trace($"allowed request from {ipAddr}");
            await _next(context);
        }
    }
}