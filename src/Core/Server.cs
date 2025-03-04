using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Herta.Utils.Logger;
using Herta.Extensions.AutoServiceRegExt;
using Herta.Extensions.AutoMiddlewareRegExt;
using Herta.Extensions.AutoAuthRegExt;
using Herta.Core.Contexts.DBContext;
using Herta.Security.Requirements.JwtRequire;

using Pomelo.EntityFrameworkCore.MySql;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;


namespace Herta.Core.Server;

public class HertaApiServer
{
    private WebApplicationBuilder _builder;
    private WebApplication _app;
    private bool _isDevelopment;
    private static NLog.ILogger _logger = LoggerManager.GetLogger(typeof(HertaApiServer));
    private bool _useDefaultDb;
    private bool _ignoreConfigWarn = false;
    private bool _useDefaultAuth;
    public event Action<WebApplication>? OnConfigure;
    public event Action? OnStart;
    public event Action? OnStop;

    public HertaApiServer(bool debug = false, bool IgnoreConfigureWarning = false, bool UseDefaultDb = true, bool UseDefaultAuth = true, WebApplicationBuilder? builder = null, WebApplication? app = null)
    {
        _useDefaultDb = UseDefaultDb;
        _useDefaultAuth = UseDefaultAuth;
        _builder = builder ?? WebApplication.CreateBuilder(
            new WebApplicationOptions
            {
                EnvironmentName = debug ? "Development" : "Production"
            });

        _isDevelopment = debug;
        _ignoreConfigWarn = IgnoreConfigureWarning;
        Initialize();
        _app = app ?? _builder.Build();
    }

    private void InitLogger()
    {
        _builder.Logging.ClearProviders(); // 清除默认日志提供程序
        LoggerManager.Initialize(_builder.Configuration); // 初始化日志管理器
    }

    private void Initialize()
    {
        // 检查开发者有没有自定义日志配置，不能覆盖开发者的配置
        bool hasCustomLogger = _builder.Configuration.GetSection("Logging").GetChildren().Any();

        if (!hasCustomLogger) // using my logger if the developer has not his own logger
        {
            InitLogger();
        }
        // fatal exception hook setting.
        OnFatalException();
        // Configure services
        BuildServices();
    }

    private void ConfigWarn(string? logMsg)
    {
        if (!_ignoreConfigWarn)
        {
            _logger.Warn($"{logMsg}, to ignore this warning, set the IgnoreConfigureWarning to true in HertaApiServer constructor.");
        }
    }
    private void BuildServices()
    {
        // swich mysql database
        if (_useDefaultDb)
        {
                var connectionString = _builder.Configuration["ConnectionStrings:DefaultConnection"];
            _builder.Services.AddDbContext<ApplicationDbContext>(
                options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
            );
            ConfigWarn("Attention: HertaApiServer-constructor: UseDefaultDb is true , you need set it to false to use your own database.");
        }

        // Add services to the container
        _builder.Services.AddControllers();
        _builder.Services.AutoRegisterServices();
        _builder.Services.AddEndpointsApiExplorer();
        _builder.Services.AddHttpContextAccessor();
        if (_useDefaultAuth)
        {
            _builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_builder.Configuration["Jwt:Key"] ?? "secret_key")),
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });
            ConfigWarn("Attention: HertaApiServer-constructor: UseDefaultAuth is true , you need set it to false to use your own authentication.");
        }

        _builder.Services.AddAutoAuthorizationPolicies();

        // Add CORS support
        _builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            });
        });
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
        _app.UseCors();
        _app.MapControllers();
        _app.UseWebSockets(new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromMinutes(3), // 3分钟确认客户端是否还在线
        });
        _app.UseAutoMiddleware();
        OnConfigure?.Invoke(_app); // 允许开发者自定义配置
    }

    private void LifeTime()
    {
        Configure();
        OnStart?.Invoke();
        _logger.Info("Server started.");
        _app.Run();
        OnStop?.Invoke();
        _logger.Info("Server stopped.");
    }

    public void Run()
    {
        LifeTime();
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
