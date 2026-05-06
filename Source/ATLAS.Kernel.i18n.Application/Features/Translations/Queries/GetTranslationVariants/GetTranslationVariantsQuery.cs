using ATLAS.Kernel.i18n.Domain.DTOs;

namespace ATLAS.Kernel.i18n.Application.Features.Translations.Queries.GetTranslationVariants;

/// <summary>
/// 
/// </summary>
/// <param name="Code"></param>
public record GetTranslationVariantsQuery(string Code) : IRequest<Result<IReadOnlyList<TranslationDto>>>;
