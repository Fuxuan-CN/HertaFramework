
using System;
using System.Threading;

namespace Herta.Utils.TokenBucket;

public class TokenBucket
{
    private readonly int _rate; // 每秒生成的令牌数
    private readonly int _capacity; // 桶的容量
    private int _tokens; // 当前桶中的令牌数
    private DateTime _lastRefillTime; // 上次填充令牌的时间

    public TokenBucket(int rate, int capacity)
    {
        _rate = rate;
        _capacity = capacity;
        _tokens = capacity; // 初始时桶是满的
        _lastRefillTime = DateTime.UtcNow;
    }

    public bool TryConsumeToken()
    {
        // 计算自上次填充以来经过的时间
        var now = DateTime.UtcNow;
        var elapsedTime = now - _lastRefillTime;

        // 计算应该添加的令牌数
        var newTokens = (int)(elapsedTime.TotalSeconds * _rate);
        if (newTokens > 0)
        {
            // 添加新令牌，但不超过桶的容量
            _tokens = Math.Min(_tokens + newTokens, _capacity);
            _lastRefillTime = now;
        }

        // 尝试消耗一个令牌
        if (_tokens > 0)
        {
            Interlocked.Decrement(ref _tokens); // 线程安全地减少令牌
            return true;
        }

        return false;
    }
}
