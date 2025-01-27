using Microsoft.AspNetCore.Mvc;
using NLog;
using Herta.Utils.Logger;
using Herta.Models.Response;

namespace HerTa.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HelloController : ControllerBase
    {
        private static readonly NLog.ILogger log = LoggerManager.GetLogger(typeof(HelloController));

        [HttpGet("/response")]
        public Response TestResponse()
        {
            log.Info("HelloController.TestResponse() called");
            return new Response("Hello, world!");
        }
    }
}