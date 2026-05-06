using ATLAS.Kernel.i18n.Domain.DTOs;

namespace ATLAS.Kernel.i18n.Application.Features.Locales.Queries.GetLocaleConfiguration;

/// <summary>
/// 
/// </summary>
/// <param name="LanguageCode"></param>
public record GetLocaleConfigurationQuery(string LanguageCode) : IRequest<Result<LocaleConfigurationDto>>, ICacheableRequest
{
    /// <summary>
    /// 
    /// </summary>
    public string CacheKey => I18nCacheKeys.LocaleConfig(LanguageCode);

    /// <summary>
    /// 
    /// </summary>
    public TimeSpan? CacheDuration => TimeSpan.FromHours(6);
}
