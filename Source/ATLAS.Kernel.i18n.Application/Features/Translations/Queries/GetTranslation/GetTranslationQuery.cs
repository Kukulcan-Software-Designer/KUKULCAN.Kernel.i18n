using ATLAS.Kernel.i18n.Domain.DTOs;

namespace ATLAS.Kernel.i18n.Application.Features.Translations.Queries.GetTranslation;

/// <summary>
/// Returns the translated text for a code + language combination, applying the
/// BCP-47 fallback chain automatically (<c>es-ES → es → en</c>).
/// </summary>
/// <param name="Code">Translation code, e.g. <c>"CRM0001"</c>.</param>
/// <param name="LanguageCode">BCP-47 language tag, e.g. <c>"es-ES"</c>.</param>
public record GetTranslationQuery(string Code, string LanguageCode) : IRequest<Result<TranslationLookupDto>>;
