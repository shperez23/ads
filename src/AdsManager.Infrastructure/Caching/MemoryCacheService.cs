using AdsManager.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace AdsManager.Infrastructure.Caching;

public sealed class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly HashSet<string> _keys = [];
    private readonly SemaphoreSlim _keysLock = new(1, 1);

    public MemoryCacheService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        if (_memoryCache.TryGetValue(key, out T? cached) && cached is not null)
            return cached;

        var value = await factory(cancellationToken);

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        };

        options.RegisterPostEvictionCallback(static (cacheKey, _, _, state) =>
        {
            if (cacheKey is not string keyToRemove || state is not MemoryCacheService cache)
                return;

            cache._keysLock.Wait();
            try
            {
                cache._keys.Remove(keyToRemove);
            }
            finally
            {
                cache._keysLock.Release();
            }
        }, this);

        _memoryCache.Set(key, value, options);

        await _keysLock.WaitAsync(cancellationToken);
        try
        {
            _keys.Add(key);
        }
        finally
        {
            _keysLock.Release();
        }

        return value;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove(key);

        await _keysLock.WaitAsync(cancellationToken);
        try
        {
            _keys.Remove(key);
        }
        finally
        {
            _keysLock.Release();
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        await _keysLock.WaitAsync(cancellationToken);
        List<string> keysToDelete;
        try
        {
            keysToDelete = _keys.Where(x => x.StartsWith(prefix, StringComparison.Ordinal)).ToList();
            foreach (var key in keysToDelete)
                _keys.Remove(key);
        }
        finally
        {
            _keysLock.Release();
        }

        foreach (var key in keysToDelete)
            _memoryCache.Remove(key);
    }
}
