namespace CopyTradeMarketApi.Shared.Abstractions;

public interface ICacheService
{
    Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T?>> factory, TimeSpan ttl);
    void Remove(string key);
}
