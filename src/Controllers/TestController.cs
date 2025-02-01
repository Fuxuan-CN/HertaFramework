using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Herta.Utils.Logger;
using NLog;
using Herta.Exceptions;
using Herta.Responses.Response;
using Herta.Responses.JsonResponse;
using Herta.Responses.FileResponse;
using Herta.Responses.HtmlResponse;
using Herta.Responses.TextResponse;
using Herta.Responses.StreamResponse;
using Herta.Decorators.Websocket;
using Herta.Responses.RedirectResponse;
using System;
using System.Linq;
using System.Threading;
using Herta.Utils.WebsocketCreator;

namespace Herta.Controllers
{
    [ApiController] // 标记为 API 控制器
    [Route("api/test")] // 定义路由前缀为控制器名称
    public class TestController : ControllerBase
    {
        private static readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(TestController));
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        [Websocket("ws")]
        public async Task HandlerWs(WebsocketManager Websocket)
        {
            _logger.Info("访问了 WeatherForecastController 的 HandlerWs 方法");
            while (true)
            {
                try
                {
                    string message = await Websocket.ReceiveTextAsync();
                    if (message == "close")
                    {
                        await Websocket.CloseAsync(1000, "Closed by client.");
                        break;
                    }
                    await Websocket.SendTextAsync($"Echo: {message}");
                }
                catch (Exception ex)
                {
                    await Websocket.CloseAsync(1000, $"Error occurred: {ex.Message}");
                    break;
                }
            }
        }

        [HttpGet("redicted")]
        public RedirectResponse Redirected()
        {
            _logger.Info("访问了 WeatherForecastController 的 Redirected 方法");
            return new RedirectResponse("https://www.bilibili.com/");
        }

        [HttpGet("weather")] // 定义一个 GET 请求方法
        public JsonResponse Get()
        {
            _logger.Info("访问了 WeatherForecastController 的 Get 方法");
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
            _logger.Info("访问了 WeatherForecastController 的 Error 方法");
            throw new HttpException(StatusCodes.Status404NotFound, "Not Found");
        }

        [HttpGet("file")]
        public FileResponse GetFile()
        {
            _logger.Info("访问了 WeatherForecastController 的 GetFile 方法");
            var file = new FileResponse(@"F:\Csharp\testApp\Herta\src\resources\hacknet_labyrinths_ost_sabotage.wav", "test.wav");
            return file;
        }

        [HttpGet("html")]
        public HtmlResponse GetHtml()
        {
            _logger.Info("访问了 WeatherForecastController 的 GetHtml 方法");
            var html = new HtmlResponse("<h1>Hello World</h1>");
            return html;
        }

        [HttpGet("text")]
        public TextResponse GetText()
        {
            _logger.Info("访问了 WeatherForecastController 的 GetText 方法");
            var text = new TextResponse("Hello World");
            return text;
        }

        [HttpGet("stream")]
        public StreamResponse GetStream()
        {
            _logger.Info("访问了 WeatherForecastController 的 GetStream 方法");
            var stream = new StreamResponse(System.IO.File.OpenRead(@"F:\Csharp\testApp\Herta\src\resources\hacknet_labyrinths_ost_sabotage.wav"));
            return stream;
        }
    }
}