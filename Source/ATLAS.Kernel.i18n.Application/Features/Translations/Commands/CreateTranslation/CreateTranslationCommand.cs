using ATLAS.Kernel.i18n.Domain.DTOs;

namespace ATLAS.Kernel.i18n.Application.Features.Translations.Commands.CreateTranslation;

/// <summary>
/// 
/// </summary>
/// <param name="Code"></param>
/// <param name="LanguageCode"></param>
/// <param name="Text"></param>
/// <param name="Context"></param>
/// <param name="MaxLength"></param>
public record CreateTranslationCommand(string Code, string LanguageCode, string Text, string? Context = null, int? MaxLength = null) : IRequest<Result<TranslationDto>>;

