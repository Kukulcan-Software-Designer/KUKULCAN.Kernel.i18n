using KUKULCAN.Kernel.i18n.Domain.DTOs;

namespace KUKULCAN.Kernel.i18n.Application.Features.Translations.Queries.GetTranslationVariants;

/// <summary>
/// Represents the GetTranslationVariantsQuery record.
/// </summary>
/// <param name="Code">The Code parameter.</param>
public record GetTranslationVariantsQuery(string Code) : IRequest<Result<IReadOnlyList<TranslationDto>>>;
