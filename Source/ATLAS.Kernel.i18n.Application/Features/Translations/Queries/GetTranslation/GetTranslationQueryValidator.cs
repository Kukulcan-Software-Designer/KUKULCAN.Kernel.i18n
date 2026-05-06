using FluentValidation;

namespace ATLAS.Kernel.i18n.Application.Features.Translations.Queries.GetTranslation;

/// <summary>
/// 
/// </summary>
public sealed class GetTranslationQueryValidator : AbstractValidator<GetTranslationQuery>
{
    /// <summary>
    /// 
    /// </summary>
    public GetTranslationQueryValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Translation code is required.")
            .Must(c => TranslationCode.From(c).IsSuccess)
            .WithMessage("Translation code must follow the format MODULE + 4 digits (e.g. CRM0001).");

        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Language code is required.")
            .Must(lc => LanguageCode.Create(lc).IsSuccess)
            .WithMessage("Language code must be a valid BCP-47 tag (e.g. es-ES, en-US).");
    }
}
