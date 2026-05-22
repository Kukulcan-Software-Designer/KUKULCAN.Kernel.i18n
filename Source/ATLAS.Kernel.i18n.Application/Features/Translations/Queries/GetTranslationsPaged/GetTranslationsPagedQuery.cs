using ATLAS.Kernel.i18n.Domain.DTOs;

namespace ATLAS.Kernel.i18n.Application.Features.Translations.Queries.GetTranslationsPaged;

/// <summary>
/// Returns a paged list of translations for admin tooling.
/// Uses <see cref="PaginationRequest"/> from <c>Atlas.SharedKernel.Infrastructure</c>.
/// </summary>
/// <param name="Pagination">The Pagination parameter.</param>
/// <param name="ModuleFilter">The ModuleFilter parameter.</param>
/// <param name="LanguageFilter">The LanguageFilter parameter.</param>
public record GetTranslationsPagedQuery(PaginationRequest Pagination, string? ModuleFilter = null, string? LanguageFilter = null) : IRequest<Result<PagedResult<TranslationDto>>>;
