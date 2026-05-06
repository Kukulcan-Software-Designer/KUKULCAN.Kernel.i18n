using FluentValidation;

namespace ATLAS.Kernel.i18n.Application.Features.Translations.Queries.GetTranslationsByModule;

/// <summary>
/// 
/// </summary>
public sealed class GetTranslationsByModuleQueryValidator : AbstractValidator<GetTranslationsByModuleQuery>
{
    /// <summary>
    /// 
    /// </summary>
    public GetTranslationsByModuleQueryValidator()
    {
        RuleFor(x => x.Module)
            .NotEmpty()
            .MinimumLength(TranslationCode.MinModuleLength)
            .MaximumLength(TranslationCode.MaxModuleLength)
            .Matches("^[a-zA-Z]+$").WithMessage("Module must contain only letters.");

        RuleFor(x => x.LanguageCode)
            .NotEmpty()
            .Must(lc => LanguageCode.Create(lc).IsSuccess)
            .WithMessage("Language code must be a valid BCP-47 tag.");
    }
}
