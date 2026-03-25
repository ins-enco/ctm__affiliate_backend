namespace CopyTradeMarketApi.Shared.Cache;

public class RedisCacheService(IDistributedCache redis) : ICacheService
{
    public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T?>> factory, TimeSpan ttl)
    {
        var cached = await redis.GetStringAsync(key);
        if (cached is not null)
            return JsonSerializer.Deserialize<T>(cached);

        var value = await factory();
        if (value is not null)
            await redis.SetStringAsync(key, JsonSerializer.Serialize(value),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });

        return value;
    }

    public void Remove(string key) => redis.Remove(key);
}
