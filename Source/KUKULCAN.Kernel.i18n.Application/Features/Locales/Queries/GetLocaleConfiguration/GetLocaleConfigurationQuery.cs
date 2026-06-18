using KUKULCAN.Kernel.i18n.Domain.DTOs;
using KUKULCAN.Kernel.i18n.Application.Common;

namespace KUKULCAN.Kernel.i18n.Application.Features.Locales.Queries.GetLocaleConfiguration;

/// <summary>
/// Represents the GetLocaleConfigurationQuery record.
/// </summary>
/// <param name="LanguageCode">The LanguageCode parameter.</param>
public record GetLocaleConfigurationQuery(string LanguageCode) : IRequest<Result<LocaleConfigurationDto>>, ICacheableRequest
{
    /// <summary>
    /// Gets the cache key associated with the current language code.
    /// </summary>
    /// <remarks>Use this key to store or retrieve language-specific data from a cache. The value is generated
    /// based on the language code and is unique per language.</remarks>
    public string CacheKey => I18NCacheKeys.Language(LanguageCode);

    /// <summary>
    /// Gets the duration for which items are cached before expiration, if caching is enabled.
    /// </summary>
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(6);
}
