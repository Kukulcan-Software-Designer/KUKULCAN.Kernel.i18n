using ATLAS.Kernel.i18n.Domain.ValueObjects.Enums;
using FluentValidation;

namespace ATLAS.Kernel.i18n.Application.Features.Locales.Commands.UpsertLocaleConfiguration;

/// <summary>
/// Provides validation rules for the UpsertLocaleConfigurationCommand to ensure that locale configuration data meets
/// required formats and constraints.
/// </summary>
/// <remarks>This validator enforces rules such as valid BCP-47 language codes, non-empty and length-limited date
/// and time formats, valid first day of the week values, and distinct single-character decimal and thousands
/// separators. It is intended to be used with FluentValidation to validate locale configuration commands before
/// processing.</remarks>
public sealed class UpsertLocaleConfigurationCommandValidator : AbstractValidator<UpsertLocaleConfigurationCommand>
{
    /// <summary>
    /// 
    /// </summary>
    public UpsertLocaleConfigurationCommandValidator()
    {
        RuleFor(x => x.LanguageCode)
            .NotEmpty()
            .Must(lc => LanguageCode.Create(lc).IsSuccess)
            .WithMessage("Language code must be a valid BCP-47 tag.");

        RuleFor(x => x.DateFormat).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ShortDateFormat).NotEmpty().MaximumLength(50);
        RuleFor(x => x.TimeFormat).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DateTimeFormat).NotEmpty().MaximumLength(100);

        RuleFor(x => x.FirstDayOfWeek)
            .NotEmpty()
            .Must(v => Enum.TryParse<FirstDayOfWeek>(v, true, out _))
            .WithMessage("FirstDayOfWeek must be 'Sunday', 'Monday', or 'Saturday'.");

        RuleFor(x => x.DecimalSeparator).NotEmpty().Length(1);
        RuleFor(x => x.ThousandsSeparator).NotEmpty().Length(1);

        RuleFor(x => x)
            .Must(x => x.DecimalSeparator != x.ThousandsSeparator)
            .WithMessage("DecimalSeparator and ThousandsSeparator must be different characters.");

        RuleFor(x => x.DecimalPlaces).InclusiveBetween(0, 10);
        RuleFor(x => x.CurrencyDecimalPlaces).InclusiveBetween(0, 10);
    }
}
