namespace KUKULCAN.Kernel.i18n.Application.Features.Translations.Commands.DeleteTranslation;

/// <summary>
/// Represents the DeleteTranslationCommand record.
/// </summary>
/// <param name="Code">The Code parameter.</param>
/// <param name="LanguageCode">The LanguageCode parameter.</param>
public record DeleteTranslationCommand(string Code, string LanguageCode) : IRequest<Result>;
