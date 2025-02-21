using Microsoft.AspNetCore.Mvc;
using NLog;
using Herta.Utils.Logger;
using Herta.Responses.Response;
using Herta.Decorators.Websocket;
using Herta.Utils.HertaWebsocketUtil;
using Herta.Decorators.Security;
using Herta.Security.MiddlewarePolicy.ExampleSecurityPolicy;

namespace HerTa.Controllers.HelloController
{
    // 测试用的HelloController
    [ApiController]
    [Route("api/hello")]
    public class HelloController : ControllerBase
    {
        private static readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(HelloController));

        [HttpGet]
        [SecurityProtect(true, typeof(ExampleSecurityPolicy))]
        public Response TestResponse()
        {
            _logger.Info("HelloController.TestResponse() called");
            return new Response("Hello, world!");
        }

        [Websocket("echo/{id}")]
        public async Task TestWebsocket(HertaWebsocket websocket)
        {
            _logger.Info("HelloController.TestWebsocket() called");
            await websocket.SendTextAsync($"Hello, world!, from id is {websocket.Parameters["id"]}, and query is {websocket.Parameters["e"]}.");
            await websocket.CloseAsync(1000, "Goodbye!");
        }
    }
}
