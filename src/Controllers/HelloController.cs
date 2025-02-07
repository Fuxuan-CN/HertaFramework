using Microsoft.AspNetCore.Mvc;
using NLog;
using Herta.Utils.Logger;
using Herta.Responses.Response;
using Herta.Decorators.Websocket;
using Herta.Utils.HertaWebsocket;

namespace HerTa.Controllers.HelloController
{
    // 测试用的HelloController
    [ApiController]
    [Route("api/hello")]
    public class HelloController : ControllerBase
    {
        private static readonly NLog.ILogger log = LoggerManager.GetLogger(typeof(HelloController));

        [HttpGet]
        public Response TestResponse()
        {
            log.Info("HelloController.TestResponse() called");
            return new Response("Hello, world!");
        }

        [Websocket("ws")]
        public async Task TestWebsocket(HertaWebsocket websocket)
        {
            log.Info("HelloController.TestWebsocket() called");
            await websocket.SendTextAsync("Hello, world!");
            await websocket.CloseAsync(1000, "Goodbye!");
        }
    }
}
