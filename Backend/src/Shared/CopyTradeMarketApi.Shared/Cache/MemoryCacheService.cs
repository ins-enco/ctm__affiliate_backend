namespace CopyTradeMarketApi.Shared.Cache;

public class MemoryCacheService(IMemoryCache cache) : ICacheService
{
    public Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T?>> factory, TimeSpan ttl)
        => cache.GetOrCreateAsync<T?>(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ttl;
            return await factory();
        })!;

    public void Remove(string key) => cache.Remove(key);
}
