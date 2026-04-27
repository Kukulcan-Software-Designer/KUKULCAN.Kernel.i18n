using ATLAS.i18n.Domain.SeedWork;

namespace ATLAS.i18n.Application.Common.Interfaces;

/// <summary>
/// Unit of Work — coordinates the saving of all repositories in a single transaction.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>
/// Abstraction over the distributed/in-memory cache used to avoid
/// hitting the database on every translation request.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        where T : class;

    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpiry = null,
        CancellationToken ct = default)
        where T : class;

    Task RemoveAsync(string key, CancellationToken ct = default);

    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
}

/// <summary>
/// Dispatches domain events to their registered handlers after a successful commit.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken ct = default);
}

/// <summary>
/// Cache key constants used across the application layer.
/// </summary>
public static class CacheKeys
{
    public const string LanguagesAll     = "i18n:languages:all";
    public const string LanguagesActive  = "i18n:languages:active";
    public const string LanguageDefault  = "i18n:languages:default";

    public static string Language(string code)
        => $"i18n:language:{code.ToUpperInvariant()}";

    public static string Translation(string code, string lang)
        => $"i18n:translation:{code.ToUpperInvariant()}:{lang.ToUpperInvariant()}";

    public static string TranslationsModule(string module, string lang)
        => $"i18n:translations:{module.ToUpperInvariant()}:{lang.ToUpperInvariant()}";

    public static string LocaleConfig(string lang)
        => $"i18n:locale:{lang.ToUpperInvariant()}";

    public static string CurrencyFormat(string lang, string currency)
        => $"i18n:currency:{lang.ToUpperInvariant()}:{currency.ToUpperInvariant()}";

    public static string CurrencyFormatsForLanguage(string lang)
        => $"i18n:currencies:{lang.ToUpperInvariant()}";

    /// <summary>Prefix used to invalidate all translation cache keys for a language.</summary>
    public static string TranslationPrefix(string lang)
        => $"i18n:translation:";
}
