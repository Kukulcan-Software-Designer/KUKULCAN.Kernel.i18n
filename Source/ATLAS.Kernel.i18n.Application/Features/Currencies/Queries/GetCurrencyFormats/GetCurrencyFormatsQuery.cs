using ATLAS.Kernel.i18n.Domain.DTOs;

namespace ATLAS.Kernel.i18n.Application.Features.Currencies.Queries.GetCurrencyFormats;

/// <summary>
/// Represents a query to retrieve the list of currency formats for a specified language.
/// </summary>
/// <remarks>Implements caching with a default duration of six hours. The cache key is generated based on the
/// provided language code.</remarks>
/// <param name="LanguageCode">The language code, in ISO 639-1 format, for which to retrieve currency formats. Cannot be null or empty.</param>
public record GetCurrencyFormatsQuery(string LanguageCode) : IRequest<Result<IReadOnlyList<CurrencyFormatDto>>>, ICacheableRequest
{
    /// <summary>
    /// Gets the cache key used to retrieve currency format data for the specified language code.
    /// </summary>
    /// <remarks>Use this key when accessing or storing currency format information in a cache to ensure
    /// language-specific data is correctly identified.</remarks>
    public string CacheKey => I18nCacheKeys.CurrencyFormats(LanguageCode);

    /// <summary>
    /// Gets the duration for which items are cached before expiration, if caching is enabled.
    /// </summary>
    public TimeSpan? CacheDuration => TimeSpan.FromHours(6);
}
