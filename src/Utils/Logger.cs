using System.Collections.Concurrent;
using System.IO;
using Microsoft.Extensions.Configuration;
using NLog;

namespace Herta.Utils.Logger
{
    public sealed class LoggerManager
    {
        private static readonly ConcurrentDictionary<string, NLog.ILogger> _logs = new ConcurrentDictionary<string, NLog.ILogger>();
        private static IConfiguration? _configuration;
        private static bool _isInitialized = false;

        // 静态方法来设置 IConfiguration
        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;
            InitializeLogger();
        }

        // 延迟初始化日志配置
        private static void InitializeLogger()
        {
            if (_isInitialized) return;

            if (_configuration == null)
            {
                throw new InvalidOperationException("Configuration has not been initialized.");
            }

            string logConfPath = _configuration["NlogConfigPath"] ?? string.Empty;
            string logConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logConfPath);

            if (!File.Exists(logConfigFilePath))
            {
                throw new FileNotFoundException($"Logger config file not found: {logConfigFilePath}");
            }

            LogManager.Setup().LoadConfigurationFromFile(logConfigFilePath);
            _isInitialized = true;
        }

        public static NLog.ILogger GetLogger(object name)
        {
            if (!_isInitialized)
            {
                InitializeLogger();
            }

            return _logs.GetOrAdd(name?.ToString() ?? "Unknown", LogManager.GetLogger);
        }
    }
}
