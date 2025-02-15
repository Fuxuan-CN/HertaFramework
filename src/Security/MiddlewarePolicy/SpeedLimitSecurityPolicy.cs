using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Herta.Interfaces.ISecurityPolicy;
using Microsoft.AspNetCore.Http;

namespace Herta.Security.MiddlewarePolicy.SpeedLimitSecurityPolicy
{
    public class SpeedLimitSecurityPolicy : ISecurityPolicy
    {
        private readonly ConcurrentDictionary<string, Queue<DateTime>> _requestTimestamps = new();
        private readonly int _maxRequestsPerSecond; // 每秒最大请求数
        private readonly TimeSpan _timeWindow = TimeSpan.FromSeconds(1); // 时间窗口为1秒

        public SpeedLimitSecurityPolicy(int maxRequestsPerSecond = 100)
        {
            _maxRequestsPerSecond = maxRequestsPerSecond;
        }

        public Task<int> GetStatusCode()
        {
            return Task.FromResult(429); // Too Many Requests
        }

        public Task<string?> GetBlockedReason()
        {
            return Task.FromResult<string?>($"sorry, but you have exceeded the maximum number of requests per second. limit is : {_maxRequestsPerSecond} per {_timeWindow.TotalSeconds} seconds.");
        }

        public Task<bool> IsRequestAllowed(HttpContext context)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var now = DateTime.UtcNow;

            // 获取或初始化该IP的请求时间戳队列
            if (!_requestTimestamps.TryGetValue(ip, out var timestamps))
            {
                timestamps = new Queue<DateTime>();
                _requestTimestamps[ip] = timestamps;
            }

            // 清理超出时间窗口的旧时间戳
            while (timestamps.Count > 0 && timestamps.Peek() < now.Subtract(_timeWindow))
            {
                timestamps.Dequeue();
            }

            // 检查当前请求是否超出限制
            if (timestamps.Count >= _maxRequestsPerSecond)
            {
                return Task.FromResult(false); // 超出限制，拒绝请求
            }

            // 添加当前请求的时间戳
            timestamps.Enqueue(now);
            return Task.FromResult(true); // 请求通过
        }
    }
}