using ATLAS.Kernel.i18n.Domain.DTOs;

namespace ATLAS.Kernel.i18n.Application.Features.Translations.Commands.UpdateTranslation;

/// <summary>
/// Represents the UpdateTranslationCommand record.
/// </summary>
/// <param name="Code">The Code parameter.</param>
/// <param name="LanguageCode">The LanguageCode parameter.</param>
/// <param name="NewText">The NewText parameter.</param>
/// <param name="NewContext">The NewContext parameter.</param>
public record UpdateTranslationCommand(string Code, string LanguageCode, string NewText, string? NewContext = null) : IRequest<Result<TranslationDto>>;
