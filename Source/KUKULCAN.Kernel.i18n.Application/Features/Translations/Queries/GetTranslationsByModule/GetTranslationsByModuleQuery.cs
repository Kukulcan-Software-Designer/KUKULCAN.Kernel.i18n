using KUKULCAN.Kernel.i18n.Domain.DTOs;
using KUKULCAN.Kernel.i18n.Application.Common;

namespace KUKULCAN.Kernel.i18n.Application.Features.Translations.Queries.GetTranslationsByModule;

/// <summary>
/// Returns all translations for a module and language as a flat dictionary.
/// Missing individual translations are filled by the BCP-47 fallback chain.
/// </summary>
/// <param name="Module">Module prefix, e.g. <c>"CRM"</c>, <c>"PIM"</c>.</param>
/// <param name="LanguageCode">BCP-47 language tag, e.g. <c>"es-ES"</c>.</param>
public record GetTranslationsByModuleQuery(string Module, string LanguageCode) : IRequest<Result<TranslationMapDto>>, ICacheableRequest
{
    /// <summary>
    /// Gets the cache key associated with the current language code.
    /// </summary>
    /// <remarks>Use this key to store or retrieve language-specific data from a cache. The value is generated
    /// based on the language code and is unique per language.</remarks>
    public string CacheKey => I18NCacheKeys.ModuleTranslations(Module, LanguageCode);

    /// <summary>
    /// Gets the duration for which items are cached before expiration, if caching is enabled.
    /// </summary>
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(1);
}
