using Microsoft.AspNetCore.Mvc;
using NLog;
using Herta.Utils.Logger;
using Herta.Responses.Response;

namespace HerTa.Controllers
{
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
    }
}
