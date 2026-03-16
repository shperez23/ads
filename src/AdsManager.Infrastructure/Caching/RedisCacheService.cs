using System.Text.Json;
using AdsManager.Application.Configuration;
using AdsManager.Application.Interfaces;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AdsManager.Infrastructure.Caching;

public sealed class RedisCacheService : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly RedisCacheOptions _options;

    public RedisCacheService(IConnectionMultiplexer connectionMultiplexer, IOptions<CacheOptions> cacheOptions)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _options = cacheOptions.Value.Redis;
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var database = _connectionMultiplexer.GetDatabase();
        var redisKey = BuildKey(key);
        var cachedValue = await database.StringGetAsync(redisKey);

        if (cachedValue.HasValue)
        {
            var deserialized = JsonSerializer.Deserialize<T>(cachedValue!, SerializerOptions);
            if (deserialized is not null)
                return deserialized;
        }

        var created = await factory(cancellationToken);
        var effectiveTtl = ttl > TimeSpan.Zero
            ? ttl
            : TimeSpan.FromSeconds(Math.Max(1, _options.DefaultTtlSeconds));

        var payload = JsonSerializer.Serialize(created, SerializerOptions);
        await database.StringSetAsync(redisKey, payload, effectiveTtl);

        return created;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var database = _connectionMultiplexer.GetDatabase();
        await database.KeyDeleteAsync(BuildKey(key));
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var endpoints = _connectionMultiplexer.GetEndPoints();
        if (endpoints.Length == 0)
            return;

        var fullPrefix = BuildKey(prefix);
        var tasks = new List<Task>();

        foreach (var endpoint in endpoints)
        {
            var server = _connectionMultiplexer.GetServer(endpoint);
            if (!server.IsConnected)
                continue;

            var keys = server.Keys(pattern: $"{fullPrefix}*").ToArray();
            if (keys.Length == 0)
                continue;

            var database = _connectionMultiplexer.GetDatabase();
            tasks.Add(database.KeyDeleteAsync(keys));
        }

        await Task.WhenAll(tasks);
    }

    private string BuildKey(string key)
        => string.IsNullOrWhiteSpace(_options.InstanceName)
            ? key
            : $"{_options.InstanceName}:{key}";
}
