using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Herta.Utils.MemoryCache
{
    public class MemoryCache<KT, VT>
    {
        private readonly ConcurrentDictionary<KT, CacheItem<VT>> _cache = new();
        private readonly Timer _cleanupTimer;

        // 定义回调事件
        public event EventHandler<KT>? ItemRemoved;

        public MemoryCache()
        {
            // 启动一个定时器，定期清理过期项
            _cleanupTimer = new Timer(_ => Cleanup(), null, 0, 60000); // 每60秒清理一次
        }

        public void Set(KT key, VT value, TimeSpan expiration)
        {
            var cacheItem = new CacheItem<VT>
            {
                Value = value,
                ExpirationTime = DateTime.UtcNow.Add(expiration)
            };
            _cache[key] = cacheItem;
        }

        public bool TryGet(KT key, out VT value)
        {
            if (_cache.TryGetValue(key, out var cacheItem) && cacheItem.ExpirationTime > DateTime.UtcNow)
            {
                value = cacheItem.Value;
                return true;
            }

            // 如果项已过期，触发回调并移除
            Remove(key);
            value = default;
            return false;
        }

        public void Remove(KT key)
        {
            if (_cache.TryRemove(key, out var cacheItem))
            {
                // 触发回调事件
                ItemRemoved?.Invoke(this, key);
            }
        }

        private void Cleanup()
        {
            foreach (var kvp in _cache)
            {
                if (kvp.Value.ExpirationTime < DateTime.UtcNow)
                {
                    Remove(kvp.Key);
                }
            }
        }

        private class CacheItem<T>
        {
            public required T Value { get; set; }
            public DateTime ExpirationTime { get; set; }
        }
    }
}