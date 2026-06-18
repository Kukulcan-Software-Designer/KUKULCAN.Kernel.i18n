using KUKULCAN.Kernel.i18n.Domain.DTOs;

namespace KUKULCAN.Kernel.i18n.Application.Features.Translations.Commands.CreateTranslation;

/// <summary>
/// Represents the CreateTranslationCommand record.
/// </summary>
/// <param name="Code">The Code parameter.</param>
/// <param name="LanguageCode">The LanguageCode parameter.</param>
/// <param name="Text">The Text parameter.</param>
/// <param name="Context">The Context parameter.</param>
/// <param name="MaxLength">The MaxLength parameter.</param>
public record CreateTranslationCommand(string Code, string LanguageCode, string Text, string? Context = null, int? MaxLength = null) : IRequest<Result<TranslationDto>>;
