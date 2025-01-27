using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Herta.Utils.Logger;
using NLog;
using Herta.Exceptions;
using Herta.Models.Response;
using Herta.Models.JsonResponse;
using Herta.Models.FileResponse;
using Herta.Models.HtmlResponse;
using Herta.Models.TextResponse;
using Herta.Models.StreamResponse;
using Herta.Models.RedirectResponse;

namespace Herta.Controllers
{
    [ApiController] // 标记为 API 控制器
    [Route("test/")] // 定义路由前缀为控制器名称
    public class TestController : ControllerBase
    {
        private static readonly NLog.ILogger log = LoggerManager.GetLogger(typeof(TestController));
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        [HttpGet("redicted")]
        public RedirectResponse Redirected()
        {
            log.Info("访问了 WeatherForecastController 的 Redirected 方法");
            return new RedirectResponse("https://www.bilibili.com/");
        }

        [HttpGet("weather")] // 定义一个 GET 请求方法
        public JsonResponse Get()
        {
            log.Info("访问了 WeatherForecastController 的 Get 方法");
            var forecast = Enumerable.Range(1, 5).Select(index => new
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            }).ToArray();

            return new JsonResponse(forecast);
        }

        [HttpGet("error")]
        public Response Error()
        {
            log.Info("访问了 WeatherForecastController 的 Error 方法");
            throw new HttpException(StatusCodes.Status404NotFound, "Not Found");
        }

        [HttpGet("file")]
        public FileResponse GetFile()
        {
            log.Info("访问了 WeatherForecastController 的 GetFile 方法");
            var file = new FileResponse(@"F:\Csharp\testApp\Herta\src\resources\hacknet_labyrinths_ost_sabotage.wav", "test.wav");
            return file;
        }

        [HttpGet("html")]
        public HtmlResponse GetHtml()
        {
            log.Info("访问了 WeatherForecastController 的 GetHtml 方法");
            var html = new HtmlResponse("<h1>Hello World</h1>");
            return html;
        }

        [HttpGet("text")]
        public TextResponse GetText()
        {
            log.Info("访问了 WeatherForecastController 的 GetText 方法");
            var text = new TextResponse("Hello World");
            return text;
        }

        [HttpGet("stream")]
        public StreamResponse GetStream()
        {
            log.Info("访问了 WeatherForecastController 的 GetStream 方法");
            var stream = new StreamResponse(System.IO.File.OpenRead(@"F:\Csharp\testApp\Herta\src\resources\hacknet_labyrinths_ost_sabotage.wav"));
            return stream;
        }
    }
}