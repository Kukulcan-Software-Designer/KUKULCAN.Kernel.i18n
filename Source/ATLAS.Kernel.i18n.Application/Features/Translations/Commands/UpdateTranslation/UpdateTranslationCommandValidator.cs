using FluentValidation;

namespace ATLAS.Kernel.i18n.Application.Features.Translations.Commands.UpdateTranslation;

/// <summary>
/// Represents the UpdateTranslationCommandValidator type.
/// </summary>
public sealed class UpdateTranslationCommandValidator : AbstractValidator<UpdateTranslationCommand>
{
    /// <summary>
    /// Executes UpdateTranslationCommandValidator.
    /// </summary>
    public UpdateTranslationCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.LanguageCode)
            .NotEmpty()
            .Must(lc => LanguageCode.Create(lc).IsSuccess)
            .WithMessage("Language code must be a valid BCP-47 tag.");
        RuleFor(x => x.NewText).NotEmpty().MaximumLength(4000);
    }
}
