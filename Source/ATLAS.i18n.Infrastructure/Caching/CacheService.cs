using System.Text.Json;
using ATLAS.i18n.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ATLAS.i18n.Infrastructure.Caching;

/// <summary>
/// Distributed cache implementation backed by Redis (primary) with a local
/// in-process memory cache as L1 for ultra-hot paths (e.g. individual translation lookups).
///
/// Cache hierarchy:
///   L1 — IMemoryCache (in-process, sub-millisecond, 5-minute TTL)
///   L2 — IDistributedCache / Redis (shared across replicas, 1-hour TTL by default)
/// </summary>
public sealed class DistributedCacheService : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented               = false,
    };

    private readonly IDistributedCache                      _distributed;
    private readonly IMemoryCache                           _memory;
    private readonly ILogger<DistributedCacheService>       _logger;

    // Tracks all keys stored so we can remove by prefix (Redis SCAN would be better
    // in production — this simple set is sufficient for services with bounded key spaces)
    private readonly HashSet<string> _keyTracker = [];
    private readonly SemaphoreSlim   _trackerLock = new(1, 1);

    public DistributedCacheService(
        IDistributedCache distributed,
        IMemoryCache memory,
        ILogger<DistributedCacheService> logger)
    {
        _distributed = distributed;
        _memory      = memory;
        _logger      = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        where T : class
    {
        // L1 — memory cache
        if (_memory.TryGetValue(key, out T? cached))
            return cached;

        // L2 — distributed cache
        try
        {
            var bytes = await _distributed.GetAsync(key, ct);
            if (bytes is null) return null;

            var value = JsonSerializer.Deserialize<T>(bytes, JsonOptions);

            // Backfill L1
            _memory.Set(key, value, TimeSpan.FromMinutes(5));

            return value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache GET failed for key '{Key}'. Returning null.", key);
            return null;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpiry = null,
        CancellationToken ct = default)
        where T : class
    {
        var expiry  = absoluteExpiry ?? TimeSpan.FromHours(1);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry
        };

        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
            await _distributed.SetAsync(key, bytes, options, ct);

            // Populate L1
            _memory.Set(key, value, TimeSpan.FromMinutes(5));

            // Track key for prefix removal
            await _trackerLock.WaitAsync(ct);
            try { _keyTracker.Add(key); }
            finally { _trackerLock.Release(); }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache SET failed for key '{Key}'.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _memory.Remove(key);

        try
        {
            await _distributed.RemoveAsync(key, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache REMOVE failed for key '{Key}'.", key);
        }

        await _trackerLock.WaitAsync(ct);
        try { _keyTracker.Remove(key); }
        finally { _trackerLock.Release(); }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        await _trackerLock.WaitAsync(ct);
        List<string> keysToRemove;
        try
        {
            keysToRemove = _keyTracker
                .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        finally { _trackerLock.Release(); }

        foreach (var key in keysToRemove)
            await RemoveAsync(key, ct);
    }
}

/// <summary>
/// Fallback in-memory-only cache for environments without Redis configured.
/// </summary>
public sealed class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public MemoryCacheService(IMemoryCache cache) => _cache = cache;

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        where T : class
    {
        _cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpiry = null,
        CancellationToken ct = default)
        where T : class
    {
        _cache.Set(key, value, absoluteExpiry ?? TimeSpan.FromHours(1));
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
        => Task.CompletedTask; // In-memory cache doesn't support prefix removal
}
