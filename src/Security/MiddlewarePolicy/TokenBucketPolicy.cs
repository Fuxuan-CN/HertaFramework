
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Herta.Interfaces.ISecurityPolicy;
using Microsoft.AspNetCore.Http;
using Herta.Utils.TokenBucket;

namespace Herta.Security.MiddlewarePolicy.TokenBucketPolicy
{
    public class TokenBucketPolicy : ISecurityPolicy
    {
        private readonly ConcurrentDictionary<string, TokenBucket> _buckets = new();
        private readonly int _rate; // 每秒生成的令牌数
        private readonly int _capacity; // 桶的容量

        public TokenBucketPolicy(int rate = 100, int capacity = 200)
        {
            _rate = rate;
            _capacity = capacity;
        }

        public Task<int> GetStatusCode()
        {
            return Task.FromResult(429); // Too Many Requests
        }

        public Task<string?> GetBlockedReason()
        {
            return Task.FromResult<string?>("Sorry, but you have exceeded the maximum number of requests per second.");
        }

        public Task<bool> IsRequestAllowed(HttpContext context)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var now = DateTime.UtcNow;

            // 获取或初始化该 IP 的令牌桶
            var bucket = _buckets.GetOrAdd(ip, _ => new TokenBucket(_rate, _capacity));

            // 尝试消耗一个令牌
            if (bucket.TryConsumeToken())
            {
                return Task.FromResult(true); // 请求通过
            }

            return Task.FromResult(false); // 超出限制，拒绝请求
        }
    }
}
