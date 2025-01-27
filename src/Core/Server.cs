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

namespace Herta.Core.Server
{
    public class HertaApiService
    {
        private WebApplicationBuilder _builder;
        private WebApplication _app;
        private bool _isDevelopment;
        private bool _needAuthentication = false;
        public static NLog.ILogger _logger = LoggerManager.GetLogger(typeof(HertaApiService));

        public HertaApiService(bool debug = false)
        {
            _builder = WebApplication.CreateBuilder(
                new WebApplicationOptions
                {
                    EnvironmentName = debug? "Development" : "Production"
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

        private void ConfigurePipeline()
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
            _app.MapHealthChecks("/health");
            _app.MapControllers();
            _app.UseGlobalExceptionHandler();
        }

        private void LifeTime()
        {
            ConfigurePipeline();
            onStartUp();
        }

        private void onStartUp()
        {
            _logger.Info("Server started.");
            _app.Run();
            onStop();
        }

        private void onStop(Action? callback = null)
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
    }
}