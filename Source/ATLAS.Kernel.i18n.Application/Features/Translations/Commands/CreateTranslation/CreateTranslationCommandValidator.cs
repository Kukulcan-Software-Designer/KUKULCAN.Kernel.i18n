using FluentValidation;

namespace ATLAS.Kernel.i18n.Application.Features.Translations.Commands.CreateTranslation;

/// <summary>
/// Represents the CreateTranslationCommandValidator type.
/// </summary>
public sealed class CreateTranslationCommandValidator : AbstractValidator<CreateTranslationCommand>
{
    /// <summary>
    /// Executes CreateTranslationCommandValidator.
    /// </summary>
    public CreateTranslationCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Must(c => TranslationCode.From(c).IsSuccess)
            .WithMessage("Translation code must follow the format MODULE + 4 digits (e.g. CRM0001).");

        RuleFor(x => x.LanguageCode)
            .NotEmpty()
            .Must(lc => LanguageCode.Create(lc).IsSuccess)
            .WithMessage("Language code must be a valid BCP-47 tag (e.g. es-ES, en-US).");

        RuleFor(x => x.Text).NotEmpty().MaximumLength(4000);

        When(x => x.MaxLength.HasValue, () =>
        {
            RuleFor(x => x.MaxLength!.Value).GreaterThan(0);
            RuleFor(x => x)
                .Must(x => !x.MaxLength.HasValue || x.Text.Length <= x.MaxLength.Value)
                .WithMessage("Text length exceeds the specified MaxLength.");
        });
    }
}
