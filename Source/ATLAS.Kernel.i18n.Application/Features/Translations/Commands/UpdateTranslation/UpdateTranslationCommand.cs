using ATLAS.Kernel.i18n.Domain.DTOs;

namespace ATLAS.Kernel.i18n.Application.Features.Translations.Commands.UpdateTranslation;

/// <summary>
/// 
/// </summary>
/// <param name="Code"></param>
/// <param name="LanguageCode"></param>
/// <param name="NewText"></param>
/// <param name="NewContext"></param>
public record UpdateTranslationCommand(string Code, string LanguageCode, string NewText, string? NewContext = null) : IRequest<Result<TranslationDto>>;

