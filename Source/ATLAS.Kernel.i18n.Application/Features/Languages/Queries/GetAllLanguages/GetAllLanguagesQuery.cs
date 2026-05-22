using ATLAS.Kernel.i18n.Domain.DTOs;

namespace ATLAS.Kernel.i18n.Application.Features.Languages.Queries.GetAllLanguages;

/// <summary>
/// Represents a query to retrieve all available languages, with an option to filter by active status.
/// </summary>
/// <remarks>This query supports caching. The cache key and duration are determined by the value of the ActiveOnly
/// parameter. Use this query to obtain a list of languages for localization or administrative purposes.</remarks>
/// <param name="ActiveOnly">true to include only active languages in the result; otherwise, false to include all languages. The default is true.</param>
public record GetAllLanguagesQuery(bool ActiveOnly = true) : IRequest<Result<IReadOnlyList<LanguageDto>>>, ICacheableRequest
{
    /// <summary>
    /// Gets the cache key used to retrieve language data based on the current filter state.
    /// </summary>
    /// <remarks>The returned cache key distinguishes between active-only and all languages, ensuring that
    /// cached results reflect the selected filter. Use this property when accessing or storing language data in a cache
    /// to maintain consistency with the filter applied.</remarks>
    public string CacheKey => ActiveOnly ? I18NCacheKeys.LanguagesActive : I18NCacheKeys.LanguagesAll;

    /// <summary>
    /// Gets the duration for which items are cached before expiration, if caching is enabled.
    /// </summary>
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);
}
