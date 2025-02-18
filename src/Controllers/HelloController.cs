using Microsoft.AspNetCore.Mvc;
using NLog;
using Herta.Utils.Logger;
using Herta.Responses.Response;
using Herta.Decorators.Websocket;
using Herta.Utils.HertaWebsocket;
using Herta.Decorators.Security;
using Herta.Security.MiddlewarePolicy.ExampleSecurityPolicy;

namespace HerTa.Controllers.HelloController
{
    // 测试用的HelloController
    [ApiController]
    [Route("api/hello")]
    public class HelloController : ControllerBase
    {
        private static readonly NLog.ILogger log = LoggerManager.GetLogger(typeof(HelloController));

        [HttpGet]
        [SecurityProtect(true, typeof(ExampleSecurityPolicy))]
        public Response TestResponse()
        {
            log.Info("HelloController.TestResponse() called");
            return new Response("Hello, world!");
        }

        [Websocket("ws/{id}")]
        public async Task TestWebsocket(HertaWebsocket websocket)
        {
            log.Info("HelloController.TestWebsocket() called");
            await websocket.SendTextAsync($"Hello, world!, from {websocket.Parameters["id"]}");
            await websocket.CloseAsync(1000, "Goodbye!");
        }
    }
}
