using ATLAS.Kernel.i18n.Domain.DTOs;

namespace ATLAS.Kernel.i18n.Application.Features.Translations.Queries.GetTranslationsPaged;

/// <summary>
/// Returns a paged list of translations for admin tooling.
/// Uses <see cref="PaginationRequest"/> from <c>Atlas.SharedKernel.Infrastructure</c>.
/// </summary>
/// <param name="Pagination"></param>
/// <param name="ModuleFilter"></param>
/// <param name="LanguageFilter"></param>
public record GetTranslationsPagedQuery(PaginationRequest Pagination, string? ModuleFilter = null, string? LanguageFilter = null) : IRequest<Result<PagedResult<TranslationDto>>>;
