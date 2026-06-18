using FluentValidation;

namespace KUKULCAN.Kernel.i18n.Application.Features.Languages.Commands.CreateLanguage;

/// <summary>
/// Provides validation rules for the CreateLanguageCommand, ensuring that language codes and names meet required
/// formats and constraints.
/// </summary>
/// <remarks>This validator enforces that the language code is a valid BCP-47 tag and that the name and native
/// name are not empty and do not exceed 100 characters. Use this class to validate CreateLanguageCommand instances
/// before processing them.</remarks>
public sealed class CreateLanguageCommandValidator : AbstractValidator<CreateLanguageCommand>
{
    /// <summary>
    /// Executes CreateLanguageCommandValidator.
    /// </summary>
    public CreateLanguageCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Must(c => LanguageCode.Create(c).IsSuccess)
            .WithMessage("Code must be a valid BCP-47 tag (e.g. es-ES, en-US, ca-ES).");

        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NativeName).NotEmpty().MaximumLength(100);
    }
}
