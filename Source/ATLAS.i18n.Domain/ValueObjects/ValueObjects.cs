using ATLAS.i18n.Domain.Exceptions;
using ATLAS.i18n.Domain.SeedWork;

namespace ATLAS.i18n.Domain.ValueObjects;

/// <summary>
/// ISO 639-1 two-letter language code (e.g. "EN", "ES", "FR").
/// Always stored and compared in upper-case.
/// </summary>
public sealed class LanguageCode : ValueObject
{
    public static readonly LanguageCode English  = new("EN");
    public static readonly LanguageCode Spanish  = new("ES");
    public static readonly LanguageCode French   = new("FR");
    public static readonly LanguageCode German   = new("DE");
    public static readonly LanguageCode Italian  = new("IT");
    public static readonly LanguageCode Portuguese = new("PT");
    public static readonly LanguageCode Catalan  = new("CA");

    public string Value { get; }

    private LanguageCode(string value) => Value = value;

    public static LanguageCode From(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new I18nDomainException("Language code cannot be empty.");

        var normalized = code.Trim().ToUpperInvariant();

        if (normalized.Length != 2)
            throw new I18nDomainException(
                $"Language code '{code}' is not a valid ISO 639-1 two-letter code.");

        if (!normalized.All(char.IsLetter))
            throw new I18nDomainException(
                $"Language code '{code}' must contain only letters.");

        return new LanguageCode(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(LanguageCode code) => code.Value;
}

/// <summary>
/// Unique translation identifier with the format {MODULE}{NNNN}.
/// MODULE = 2–5 uppercase letters identifying the ATLAS API module (e.g. CRM, PIM, WMS).
/// NNNN   = 4-digit zero-padded sequential number (0001–9999).
/// Examples: CRM0001, PIM0042, WMS9999, AUTH0010.
/// </summary>
public sealed class TranslationCode : ValueObject
{
    /// <summary>Maximum number of digits in the numeric part.</summary>
    public const int NumericPartLength = 4;

    /// <summary>Minimum module prefix length.</summary>
    public const int MinModuleLength = 2;

    /// <summary>Maximum module prefix length.</summary>
    public const int MaxModuleLength = 5;

    public string Value { get; }

    /// <summary>API module prefix (e.g. "CRM", "PIM").</summary>
    public string Module { get; }

    /// <summary>Numeric sequence within the module.</summary>
    public int Sequence { get; }

    private TranslationCode(string value, string module, int sequence)
    {
        Value    = value;
        Module   = module;
        Sequence = sequence;
    }

    /// <summary>
    /// Creates a TranslationCode from a raw string such as "CRM0001".
    /// </summary>
    public static TranslationCode From(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new I18nDomainException("Translation code cannot be empty.");

        var normalized = code.Trim().ToUpperInvariant();

        // Must end with exactly 4 digits
        if (normalized.Length < MinModuleLength + NumericPartLength)
            throw new I18nDomainException(
                $"Translation code '{code}' is too short. Expected format: MODULE + 4 digits (e.g. CRM0001).");

        var numericPart = normalized[^NumericPartLength..];
        var modulePart  = normalized[..^NumericPartLength];

        if (!numericPart.All(char.IsDigit))
            throw new I18nDomainException(
                $"Translation code '{code}' must end with {NumericPartLength} digits.");

        if (modulePart.Length < MinModuleLength || modulePart.Length > MaxModuleLength)
            throw new I18nDomainException(
                $"Module prefix '{modulePart}' must be between {MinModuleLength} and {MaxModuleLength} letters.");

        if (!modulePart.All(char.IsLetter))
            throw new I18nDomainException(
                $"Module prefix '{modulePart}' must contain only letters.");

        var sequence = int.Parse(numericPart);
        if (sequence < 1)
            throw new I18nDomainException(
                $"Sequence number in translation code '{code}' must be >= 1.");

        return new TranslationCode(normalized, modulePart, sequence);
    }

    /// <summary>
    /// Creates a TranslationCode from separate module and sequence components.
    /// </summary>
    public static TranslationCode Create(string module, int sequence)
    {
        if (string.IsNullOrWhiteSpace(module))
            throw new I18nDomainException("Module prefix cannot be empty.");

        var normalizedModule = module.Trim().ToUpperInvariant();

        if (normalizedModule.Length < MinModuleLength || normalizedModule.Length > MaxModuleLength)
            throw new I18nDomainException(
                $"Module prefix '{module}' must be between {MinModuleLength} and {MaxModuleLength} letters.");

        if (!normalizedModule.All(char.IsLetter))
            throw new I18nDomainException(
                $"Module prefix '{module}' must contain only letters.");

        if (sequence < 1 || sequence > 9999)
            throw new I18nDomainException(
                $"Sequence must be between 1 and 9999. Got: {sequence}.");

        var value = $"{normalizedModule}{sequence:D4}";
        return new TranslationCode(value, normalizedModule, sequence);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(TranslationCode code) => code.Value;
}

/// <summary>
/// Represents the position of the currency symbol relative to the numeric amount.
/// </summary>
public enum CurrencySymbolPosition
{
    /// <summary>Symbol appears before the amount: €1.234,56</summary>
    Before = 1,

    /// <summary>Symbol appears after the amount: 1.234,56 €</summary>
    After = 2
}

/// <summary>
/// The first day of the week for a given locale.
/// </summary>
public enum FirstDayOfWeek
{
    Sunday    = 0,
    Monday    = 1,
    Saturday  = 6
}
