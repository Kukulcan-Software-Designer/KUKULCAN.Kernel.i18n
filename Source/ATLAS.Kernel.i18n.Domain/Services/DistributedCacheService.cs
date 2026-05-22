using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ATLAS.Kernel.i18n.Domain.Services;

/// <summary>
/// Implements <see cref="ICacheService"/> from <c>Atlas.SharedKernel.Abstractions</c>
/// using a two-level strategy:
/// <list type="bullet">
///   <item>L1 — <see cref="IMemoryCache"/> (in-process, sub-millisecond, 5-minute TTL).</item>
///   <item>L2 — <see cref="IDistributedCache"/> backed by Redis (shared across replicas).</item>
/// </list>
///
/// <para>
/// When Redis is not configured, <see cref="MemoryOnlyCacheService"/> is registered
/// instead (single-node / development environments).
/// </para>
/// </summary>
public sealed class DistributedCacheService(IDistributedCache l2, IMemoryCache l1, ILogger<DistributedCacheService> logger) : ICacheService
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    /// <summary>
    /// Retrieves a value from the cache asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The cached value, or <c>null</c> if not found.</returns>
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        // L1 hit
        if (l1.TryGetValue(key, out T? cached)) return cached;

        // L2 hit
        try
        {
            var bytes = await l2.GetAsync(key, ct);
            if (bytes is null) return default;

            var value = JsonSerializer.Deserialize<T>(bytes, _json);
            l1.Set(key, value, TimeSpan.FromMinutes(5));
            return value;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Cache] GET failed for key '{Key}'", key);
            return default;
        }
    }

    /// <summary>
    /// Executes this member.
    /// </summary>
    /// <typeparam name="T">The type of the value to store.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="expiry">The optional expiration time.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var ttl = expiry ?? TimeSpan.FromHours(1);
        var opts = new DistributedCacheEntryOptions {
            AbsoluteExpirationRelativeToNow = ttl
        };

        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _json);
            await l2.SetAsync(key, bytes, opts, ct);
            l1.Set(key, value, TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Cache] SET failed for key '{Key}'", key);
        }
    }

    /// <summary>
    /// Removes a value from the cache asynchronously.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        l1.Remove(key);
        try { await l2.RemoveAsync(key, ct); }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Cache] REMOVE failed for key '{Key}'", key);
        }
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
        var existing = await GetAsync<T>(key, ct);
        if (existing is not null)
            return existing;

        var value = await factory(ct);
        if (value is not null)
            await SetAsync(key, value, expiry, ct);

        return value!;
    }

    /// <summary>
    /// Checks if a value exists in the cache asynchronously.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation, containing <c>true</c> if the value exists; otherwise, <c>false</c>.</returns>
    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        if (l1.TryGetValue(key, out _))
            return true;
        try
        {
            return (await l2.GetAsync(key, ct)) is not null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Removes values from the cache asynchronously based on a prefix.
    /// </summary>
    /// <param name="prefix">The prefix of the cache keys to remove.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        // Redis SCAN is not available via IDistributedCache — delegate to implementation-specific code.
        // For IMemoryCache we cannot enumerate keys, so we rely on natural TTL expiry for L1.
        // L2 (Redis) prefix removal requires Lua scripting; this no-op is intentional for IDistributedCache.
        // P
        // roduction deployments should use StackExchange.Redis directly for prefix removal.
        logger.LogDebug("[Cache] RemoveByPrefix '{Prefix}' — L1 entries will expire naturally.", prefix);
        await Task.CompletedTask;
    }
}
