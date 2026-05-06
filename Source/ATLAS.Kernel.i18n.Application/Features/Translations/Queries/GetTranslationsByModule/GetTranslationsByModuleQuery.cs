using ATLAS.Kernel.i18n.Domain.DTOs;

namespace ATLAS.Kernel.i18n.Application.Features.Translations.Queries.GetTranslationsByModule;

/// <summary>
/// Returns all translations for a module and language as a flat dictionary.
/// Missing individual translations are filled by the BCP-47 fallback chain.
/// </summary>
/// <param name="Module">Module prefix, e.g. <c>"CRM"</c>, <c>"PIM"</c>.</param>
/// <param name="LanguageCode">BCP-47 language tag, e.g. <c>"es-ES"</c>.</param>
public record GetTranslationsByModuleQuery(string Module, string LanguageCode) : IRequest<Result<TranslationMapDto>>, ICacheableRequest
{
    /// <summary>
    /// 
    /// </summary>
    public string CacheKey => I18nCacheKeys.ModuleTranslations(Module, LanguageCode);

    /// <summary>
    /// 
    /// </summary>
    public TimeSpan? CacheDuration => TimeSpan.FromHours(1);
}
