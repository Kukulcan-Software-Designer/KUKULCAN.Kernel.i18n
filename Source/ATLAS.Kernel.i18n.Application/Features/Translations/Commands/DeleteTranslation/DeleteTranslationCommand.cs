namespace ATLAS.Kernel.i18n.Application.Features.Translations.Commands.DeleteTranslation;

/// <summary>
/// 
/// </summary>
/// <param name="Code"></param>
/// <param name="LanguageCode"></param>
public record DeleteTranslationCommand(string Code, string LanguageCode) : IRequest<Result>;
