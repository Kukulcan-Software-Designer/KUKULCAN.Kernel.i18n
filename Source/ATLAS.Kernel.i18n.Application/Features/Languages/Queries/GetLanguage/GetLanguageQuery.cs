using ATLAS.Kernel.i18n.Domain.DTOs;

namespace ATLAS.Kernel.i18n.Application.Features.Languages.Queries.GetLanguage;

/// <summary>
/// Representa una consulta para obtener información de un idioma específico utilizando su código.
/// </summary>
/// <remarks>Esta consulta implementa almacenamiento en caché para mejorar el rendimiento de las solicitudes
/// repetidas del mismo idioma. El resultado se almacena en caché durante 30 minutos utilizando una clave generada a
/// partir del código de idioma.</remarks>
/// <param name="Code">El código de idioma que identifica de forma única el idioma a recuperar. No puede ser nulo ni estar vacío.</param>
public record GetLanguageQuery(string Code) : IRequest<Result<LanguageDto>>, ICacheableRequest
{
    /// <summary>
    /// Gets the cache key associated with the current language code.
    /// </summary>
    /// <remarks>Use this key to store or retrieve language-specific data from a cache. The value is generated
    /// based on the language code and is unique per language.</remarks>
    public string CacheKey => I18nCacheKeys.Language(Code);

    /// <summary>
    /// Gets the duration for which items are cached before expiration, if caching is enabled.
    /// </summary>
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);
}
