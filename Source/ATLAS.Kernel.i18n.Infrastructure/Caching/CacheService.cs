using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ATLAS.Kernel.i18n.Infrastructure.Caching;

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
public sealed class DistributedCacheService : ICacheService
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented               = false,
    };

    private readonly IDistributedCache                    _l2;
    private readonly IMemoryCache                         _l1;
    private readonly ILogger<DistributedCacheService>     _logger;

    public DistributedCacheService(
        IDistributedCache              l2,
        IMemoryCache                   l1,
        ILogger<DistributedCacheService> logger)
    {
        _l2     = l2;
        _l1     = l1;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        // L1 hit
        if (_l1.TryGetValue(key, out T? cached)) return cached;

        // L2 hit
        try
        {
            var bytes = await _l2.GetAsync(key, ct);
            if (bytes is null) return default;

            var value = JsonSerializer.Deserialize<T>(bytes, _json);
            _l1.Set(key, value, TimeSpan.FromMinutes(5));
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Cache] GET failed for key '{Key}'", key);
            return default;
        }
    }

    public async Task SetAsync<T>(
        string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var ttl  = expiry ?? TimeSpan.FromHours(1);
        var opts = new DistributedCacheEntryOptions
            { AbsoluteExpirationRelativeToNow = ttl };

        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _json);
            await _l2.SetAsync(key, bytes, opts, ct);
            _l1.Set(key, value, TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Cache] SET failed for key '{Key}'", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _l1.Remove(key);
        try { await _l2.RemoveAsync(key, ct); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Cache] REMOVE failed for key '{Key}'", key);
        }
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiry = null,
        CancellationToken ct = default)
    {
        var existing = await GetAsync<T>(key, ct);
        if (existing is not null) return existing;

        var value = await factory(ct);
        if (value is not null)
            await SetAsync(key, value, expiry, ct);

        return value!;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        if (_l1.TryGetValue(key, out _)) return true;
        try { return (await _l2.GetAsync(key, ct)) is not null; }
        catch { return false; }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        // Redis SCAN is not available via IDistributedCache — delegate to implementation-specific code.
        // For IMemoryCache we cannot enumerate keys, so we rely on natural TTL expiry for L1.
        // L2 (Redis) prefix removal requires Lua scripting; this no-op is intentional for IDistributedCache.
        // Production deployments should use StackExchange.Redis directly for prefix removal.
        _logger.LogDebug("[Cache] RemoveByPrefix '{Prefix}' — L1 entries will expire naturally.", prefix);
        await Task.CompletedTask;
    }
}

/// <summary>
/// In-process only cache — used when Redis is not configured.
/// Suitable for single-node development and integration tests.
/// </summary>
public sealed class MemoryOnlyCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public MemoryOnlyCacheService(IMemoryCache cache) => _cache = cache;

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        _cache.TryGetValue(key, out T? v);
        return Task.FromResult(v);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        _cache.Set(key, value, expiry ?? TimeSpan.FromHours(1));
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiry = null,
        CancellationToken ct = default)
    {
        if (_cache.TryGetValue(key, out T? v) && v is not null) return v;
        var value = await factory(ct);
        if (value is not null) _cache.Set(key, value, expiry ?? TimeSpan.FromHours(1));
        return value!;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        => Task.FromResult(_cache.TryGetValue(key, out _));

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
        => Task.CompletedTask;
}
