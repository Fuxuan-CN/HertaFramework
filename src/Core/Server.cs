using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Herta.Utils.Logger;
using Herta.Extensions.GlobalExceptionExt;
using Herta.Extensions.WebsocketExt;
using Microsoft.AspNetCore.WebSockets;

namespace Herta.Core.Server
{
    public class HertaApiService
    {
        private WebApplicationBuilder _builder;
        private WebApplication _app;
        private bool _isDevelopment;
        private bool _needAuthentication = false;
        public static NLog.ILogger _logger = LoggerManager.GetLogger(typeof(HertaApiService));

        public HertaApiService(bool debug = false, bool needAuthentication = false)
        {
            _needAuthentication = needAuthentication;
            _builder = WebApplication.CreateBuilder(
                new WebApplicationOptions
                {
                    EnvironmentName = debug ? "Development" : "Production"
                });

            _isDevelopment = debug;

            Initialize();
            _app = _builder.Build();
        }

        public bool IsDevelopment()
        {
            return _isDevelopment;
        }

        private void Initialize()
        {
            _builder.Logging.ClearProviders(); // 清除默认日志提供程序
            LoggerManager.Initialize(_builder.Configuration); // 初始化日志管理器
            // fatal exception hook setting.
            OnFatalException();
            // Configure services
            BuildServices();
        }

        private void BuildServices()
        {
            // Add services to the container
            _builder.Services.AddControllers();
            _builder.Services.AddEndpointsApiExplorer();

            // Add CORS support
            _builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                });
            });

            if (_isDevelopment)
            {
                _builder.Services.AddOpenApi();
            }

            // Add health checks
            _builder.Services.AddHealthChecks();
        }

        public void Build()
        {
            if (_app == null)
            {
                _app = _builder.Build();
            }
            else
            {
                return;
            }
        }

        public WebApplication GetAppInstance()
        {
            return _app;
        }

        private void Configure()
        {
            // Configure the HTTP request pipeline
            if (_isDevelopment)
            {
                _app.UseDeveloperExceptionPage();
                _app.MapOpenApi();
            }

            _app.UseHttpsRedirection();
            if (_needAuthentication)
            {
                _app.UseAuthentication(); // 使用身份验证中间件
                _app.UseAuthorization();  // 使用授权中间件
            }
            _app.UseCors();
            _app.MapControllers();
            _app.UseHealthChecks("/health");
            _app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(180)
            });
            _app.UseHertaWebSockets();
            _app.UseGlobalExceptionHandler();
        }

        private void LifeTime()
        {
            Configure();
            OnStartUp();
        }

        private void OnStartUp()
        {
            _logger.Info("Server started.");
            _app.Run();
            OnStop();
        }

        private void OnStop(Action? callback = null)
        {
            _logger.Info("Server stopped.");
            if (callback != null)
            {
                callback();
            }
        }

        public void Run()
        {
            LifeTime();
        }

        // Public method to get the IServiceCollection
        public IServiceCollection GetServices()
        {
            return _builder.Services;
        }

        // Additional methods to support controller-based routing
        public void MapControllerRoute(string pattern, string controllerName)
        {
            _app.MapControllerRoute(
                name: controllerName,
                pattern: pattern,
                defaults: new { controller = controllerName }
            );
        }

        public void MapControllers()
        {
            _app.MapControllers();
        }

        private void OnFatalException()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                // 确保 e.ExceptionObject 是 Exception 类型
                if (e.ExceptionObject is Exception ex)
                {
                    string fatalError = $"A fatal error occurred: {ex.GetType().Name}: {ex.Message}";
                    string fatalStackTrace = $"Failed stack trace: {Environment.NewLine}{ex.StackTrace}";
                    string logFatalMsg = $"{fatalError}\n{fatalStackTrace}";

                    // 记录更多上下文信息
                    string threadId = System.Threading.Thread.CurrentThread.ManagedThreadId.ToString();
                    string contextInfo = $"Thread ID: {threadId}\nApplication Domain: {AppDomain.CurrentDomain.FriendlyName}";

                    // 最终的日志消息
                    string finalLogMsg = $"{logFatalMsg}\n{contextInfo}";

                    // 记录致命错误
                    _logger.Fatal(finalLogMsg);
                }
                else
                {
                    // 如果 e.ExceptionObject 不是 Exception 类型，记录原始对象
                    string logMsg = $"A fatal error occurred: {e.ExceptionObject}";
                    _logger.Fatal(logMsg);
                }
                // 退出应用程序
                Environment.Exit(1);
            };
        }
    }
}
