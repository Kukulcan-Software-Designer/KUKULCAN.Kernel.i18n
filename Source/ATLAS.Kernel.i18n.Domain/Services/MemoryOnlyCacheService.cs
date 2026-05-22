using Microsoft.Extensions.Caching.Memory;

namespace ATLAS.Kernel.i18n.Domain.Services;

/// <summary>
/// In-process only cache — used when Redis is not configured.
/// Suitable for single-node development and integration tests.
/// </summary>
public sealed class MemoryOnlyCacheService(IMemoryCache cache) : ICacheService
{
    /// <summary>
    /// Retrieves a value from the cache asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The cached value, or <c>null</c> if not found.</returns>
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        cache.TryGetValue(key, out T? v);
        return Task.FromResult(v);
    }

    /// <summary>
    /// Stores a value in the cache asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the value to store.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="expiry">The optional expiration time.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        cache.Set(key, value, expiry ?? TimeSpan.FromHours(1));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes a value from the cache asynchronously.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        cache.Remove(key);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Retrieves a value from the cache asynchronously, or creates it using the provided factory function if it does not exist.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve or create.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">A factory function to create the value if it does not exist in the cache.</param>
    /// <param name="expiry">The optional expiration time.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The cached or newly created value.</returns>
    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        if (cache.TryGetValue(key, out T? v) && v is not null)
            return v;
        var value = await factory(ct);
        if (value is not null)
            cache.Set(key, value, expiry ?? TimeSpan.FromHours(1));
        return value!;
    }

    /// <summary>
    /// Checks if a value exists in the cache asynchronously.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation, containing <c>true</c> if the value exists; otherwise, <c>false</c>.</returns>
    public Task<bool> ExistsAsync(string key, CancellationToken ct = default) =>
        Task.FromResult(cache.TryGetValue(key, out _));

    /// <summary>
    /// Removes values from the cache asynchronously based on a prefix.
    /// </summary>
    /// <param name="prefix">The prefix of the cache keys to remove.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default) =>
        Task.CompletedTask;
}
