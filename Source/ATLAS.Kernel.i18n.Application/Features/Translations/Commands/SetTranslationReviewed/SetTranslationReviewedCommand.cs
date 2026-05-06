namespace ATLAS.Kernel.i18n.Application.Features.Translations.Commands.SetTranslationReviewed;

/// <summary>
/// 
/// </summary>
/// <param name="Code"></param>
/// <param name="LanguageCode"></param>
/// <param name="IsReviewed"></param>
public record SetTranslationReviewedCommand(string Code, string LanguageCode, bool IsReviewed) : IRequest<Result>;
