namespace KUKULCAN.Kernel.i18n.Application.Features.Translations.Commands.SetTranslationReviewed;

/// <summary>
/// Represents the SetTranslationReviewedCommand record.
/// </summary>
/// <param name="Code">The Code parameter.</param>
/// <param name="LanguageCode">The LanguageCode parameter.</param>
/// <param name="IsReviewed">The IsReviewed parameter.</param>
public record SetTranslationReviewedCommand(string Code, string LanguageCode, bool IsReviewed) : IRequest<Result>;
