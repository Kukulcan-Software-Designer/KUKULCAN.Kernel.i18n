using ATLAS.Kernel.i18n.Domain.ValueObjects.Enums;
using FluentValidation;

namespace ATLAS.Kernel.i18n.Application.Features.Currencies.Commands.UpsertCurrencyFormat;

/// <summary>
/// Provides validation rules for the UpsertCurrencyFormatCommand to ensure that all currency format properties meet
/// required standards and constraints.
/// </summary>
/// <remarks>This validator enforces rules such as valid BCP-47 language codes, ISO 4217 currency codes, symbol
/// position values, and correct usage of decimal and thousands separators. It is intended to be used with command
/// handling pipelines to prevent invalid currency format data from being processed.</remarks>
public sealed class UpsertCurrencyFormatCommandValidator : AbstractValidator<UpsertCurrencyFormatCommand>
{
    /// <summary>
    /// Executes UpsertCurrencyFormatCommandValidator.
    /// </summary>
    public UpsertCurrencyFormatCommandValidator()
    {
        RuleFor(x => x.LanguageCode)
            .NotEmpty()
            .Must(lc => LanguageCode.Create(lc).IsSuccess)
            .WithMessage("Language code must be a valid BCP-47 tag.");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().Length(3)
            .Matches("^[a-zA-Z]{3}$")
            .WithMessage("CurrencyCode must be a 3-letter ISO 4217 code (e.g. USD, EUR).");

        RuleFor(x => x.CurrencyName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(5);

        RuleFor(x => x.SymbolPosition)
            .NotEmpty()
            .Must(v => Enum.TryParse<CurrencySymbolPosition>(v, true, out _))
            .WithMessage("SymbolPosition must be 'Before' or 'After'.");

        RuleFor(x => x.DecimalSeparator).NotEmpty().Length(1);
        RuleFor(x => x.ThousandsSeparator).NotEmpty().Length(1);
        RuleFor(x => x.DecimalPlaces).InclusiveBetween(0, 10);

        RuleFor(x => x.NegativePattern)
            .NotEmpty()
            .Must(p => p.Contains("{amount}"))
            .WithMessage("NegativePattern must contain the {amount} placeholder.");

        RuleFor(x => x)
            .Must(x => x.DecimalSeparator != x.ThousandsSeparator)
            .WithMessage("DecimalSeparator and ThousandsSeparator must be different characters.");
    }
}
