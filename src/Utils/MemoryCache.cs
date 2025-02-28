using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Herta.Utils.MemoryCache;

public class MemoryCache<KT, VT> : IDisposable 
    where KT : notnull 
    where VT : class
{
    private readonly ConcurrentDictionary<KT, CacheItem<VT>> _cache = new();
    private readonly Lazy<Timer> _cleanupTimer;
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private bool _disposed = false;
    private TimeSpan _cleanUpInterval;
    public int Count => _cache.Count;
    public event EventHandler<TimeSpan>? CleanStarted;
    public event EventHandler<KT>? ItemRemoved;
    public event EventHandler<KT>? ItemAdded;
    public event EventHandler<KT>? ItemRefreshed;
    public event EventHandler<KT>? ItemExpired;

    public MemoryCache(TimeSpan? cleanUpInterval = null)
    {
        _cleanUpInterval = cleanUpInterval ?? TimeSpan.FromMinutes(1);
        _cleanupTimer = new Lazy<Timer>(() => new Timer(Cleanup, null, _cleanUpInterval, _cleanUpInterval));
    }

    public void Set(KT key, VT value, TimeSpan expiration)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MemoryCache<KT, VT>));

        var cacheItem = new CacheItem<VT>(value) // 通过构造函数初始化
        {
            ExpirationTime = DateTime.UtcNow.Add(expiration)
        };
        _cache[key] = cacheItem;
        ItemAdded?.Invoke(this, key);

        // 启动定时器（如果尚未启动）
        if (!_cleanupTimer.IsValueCreated)
        {
            _cleanupTimer.Value.Change(0, (int)_cleanUpInterval.TotalMilliseconds);
            CleanStarted?.Invoke(this, _cleanUpInterval);
        }
    }

    public VT? Get(KT key)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MemoryCache<KT, VT>));

        if (_cache.TryGetValue(key, out var cacheItem) && cacheItem.ExpirationTime > DateTime.UtcNow)
        {
            cacheItem.LastAccessTime = DateTime.UtcNow; // 更新最后访问时间
            return cacheItem.Value;
        }

        ItemExpired?.Invoke(this, key);
        Remove(key);
        return null;
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
        Remove(key);
        value = null;
        return false;
    }

    public bool Refresh(KT key, TimeSpan expOffset)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MemoryCache<KT, VT>));

        if (_cache.TryGetValue(key, out var cacheItem))
        {
            cacheItem.LastAccessTime = DateTime.UtcNow;
            cacheItem.ExpirationTime = DateTime.UtcNow.Add(expOffset);
            ItemRefreshed?.Invoke(this, key);
            return true;
        }

        return false;
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

        foreach (var pair in _cache)
        {
            if (pair.Value.ExpirationTime <= DateTime.UtcNow)
            {
                keysToRemove.Add(pair.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            Remove(key);
            ItemExpired?.Invoke(this, key);
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
                if (_cleanupTimer.IsValueCreated)
                {
                    _cleanupTimer.Value.Dispose();
                }
                _cache.Clear();
            }
            _disposed = true;
        }
    }

    private class CacheItem<T> where T : class
    {
        public T Value { get; } // 只读属性
        public DateTime ExpirationTime { get; set; }
        public DateTime LastAccessTime { get; set; }

        public CacheItem(T value) // 构造函数初始化 Value
        {
            Value = value;
            LastAccessTime = DateTime.UtcNow;
        }
    }
}
