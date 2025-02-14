using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Herta.Interfaces.ISecurityPolicy;
using Herta.Decorators.Services;

namespace Herta.Security.MiddlewarePolicy.SpeedLimitSecurityPolicy
{
    [Service(ServiceLifetime.Singleton)]
    public class SpeedLimitSecurityPolicy : ISecurityPolicy
    {
        private static readonly ConcurrentDictionary<string, int> Blocked = new ConcurrentDictionary<string, int>();
        private static readonly ConcurrentDictionary<string, int> RequestFrequency = new ConcurrentDictionary<string, int>();
        private static readonly int MaxReqPerSec = 100; // 每秒钟允许的最大请求数
        private static readonly TimeSpan RequestInterval = TimeSpan.FromSeconds(1);

        public SpeedLimitSecurityPolicy()
        {
            // 启动后台任务清理请求计数
            _ = CleanUpRequestFrequencyAsync();
        }

        public Task<bool> IsRequestAllowed(string ip)
        {
            // 检查 IP 是否在封禁列表中
            if (Blocked.ContainsKey(ip))
            {
                return Task.FromResult(false);
            }

            // 检查请求频率
            if (RequestFrequency.TryGetValue(ip, out int requestCount))
            {
                if (requestCount >= MaxReqPerSec)
                {
                    // 如果请求频率超过限制，封禁 IP
                    Blocked[ip] = 1;
                    return Task.FromResult(false);
                }
                else
                {
                    // 增加请求计数
                    RequestFrequency[ip] = requestCount + 1;
                }
            }
            else
            {
                // 初始化请求计数
                RequestFrequency[ip] = 1;
            }

            return Task.FromResult(true);
        }

        private async Task CleanUpRequestFrequencyAsync()
        {
            while (true)
            {
                await Task.Delay(RequestInterval);
                var keys = RequestFrequency.Keys.ToArray();
                foreach (var key in keys)
                {
                    RequestFrequency.TryRemove(key, out _);
                }
            }
        }
    }
}