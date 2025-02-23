using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Herta.Utils.MemoryCache
{
    public class MemoryCache<KT, VT> : IDisposable where KT : notnull where VT : class
    {
        private readonly ConcurrentDictionary<KT, CacheItem<VT>> _cache = new();
        private readonly Lazy<Timer> _cleanupTimer;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private bool _disposed = false;
        private bool _timerStarted = false;
        private TimeSpan _cleanUpInterval;
        public int Count => _cache.Count;
        public event EventHandler? CleanStarted;
        public event EventHandler<KT>? ItemRemoved;
        public event EventHandler<KT>? ItemAdded;
        public event EventHandler<KT>? ItemRefreshed;
        public event EventHandler<KT>? ItemExpired;

        public MemoryCache(TimeSpan? CleanUpInterval = null)
        {
            _cleanUpInterval = CleanUpInterval ?? TimeSpan.FromMinutes(1);
            _cleanupTimer = new Lazy<Timer>(() => new Timer(Cleanup, null, _cleanUpInterval, _cleanUpInterval));
        }

        public void Set(KT key, VT value, TimeSpan expiration)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MemoryCache<KT, VT>));

            var cacheItem = new CacheItem<VT>
            {
                Value = value ?? throw new ArgumentNullException(nameof(value)),
                ExpirationTime = DateTime.UtcNow.Add(expiration)
            };
            _cache[key] = cacheItem;
            ItemAdded?.Invoke(this, key);

            // 启动定时器（如果尚未启动）
            if (!_timerStarted)
            {
                _cleanupTimer.Value.Change(0, _cleanUpInterval.Milliseconds);
                _timerStarted = true;
                CleanStarted?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool TryGet(KT key, out VT? value)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MemoryCache<KT, VT>));

            if (_cache.TryGetValue(key, out var cacheItem) && cacheItem.ExpirationTime > DateTime.UtcNow)
            {
                value = cacheItem.Value;
                cacheItem.LastAccessTime = DateTime.UtcNow; // 更新最后访问时间
                return true;
            }

            ItemExpired?.Invoke(this, key);
            value = null;
            Remove(key);
            return false;
        }

        public void Refresh(KT key, TimeSpan expOffset)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MemoryCache<KT, VT>));

            if (_cache.TryGetValue(key, out var cacheItem))
            {
                cacheItem.LastAccessTime = DateTime.UtcNow;
                cacheItem.ExpirationTime = DateTime.UtcNow.Add(expOffset);
                ItemRefreshed?.Invoke(this, key);
            }
            else
            {
                throw new KeyNotFoundException($"Key {key} not found in cache.");
            }
        }

        public void Remove(KT key)
        {
            if (_disposed)
                return;

            if (_cache.TryRemove(key, out var cacheItem))
            {
                ItemRemoved?.Invoke(this, key);
            }
        }

        private void Cleanup(object? state)
        {
            if (_disposed || _cancellationTokenSource.Token.IsCancellationRequested)
                return;

            var keysToRemove = new List<KT>();

            foreach (var kvp in _cache)
            {
                if (kvp.Value.ExpirationTime < DateTime.UtcNow)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                Remove(key);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _cancellationTokenSource.Cancel();
                    _cleanupTimer.Value?.Dispose();
                    _cache.Clear();
                }

                _disposed = true;
            }
        }

        private class CacheItem<T> where T : class
        {
            public required T Value { get; set; }
            public DateTime ExpirationTime { get; set; }
            public DateTime LastAccessTime { get; set; }
        }
    }
}