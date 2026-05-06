using FluentValidation;

namespace ATLAS.Kernel.i18n.Application.Features.Translations.Queries.GetTranslationsPaged;

/// <summary>
/// 
/// </summary>
public sealed class GetTranslationsPagedQueryValidator : AbstractValidator<GetTranslationsPagedQuery>
{
    /// <summary>
    /// 
    /// </summary>
    public GetTranslationsPagedQueryValidator()
    {
        When(x => x.ModuleFilter is not null, () =>
            RuleFor(x => x.ModuleFilter!)
                .MinimumLength(TranslationCode.MinModuleLength)
                .MaximumLength(TranslationCode.MaxModuleLength)
                .Matches("^[a-zA-Z]+$"));

        When(x => x.LanguageFilter is not null, () =>
            RuleFor(x => x.LanguageFilter!)
                .Must(lc => LanguageCode.Create(lc).IsSuccess)
                .WithMessage("LanguageFilter must be a valid BCP-47 tag."));
    }
}
