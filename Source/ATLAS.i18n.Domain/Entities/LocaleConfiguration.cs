using ATLAS.i18n.Domain.SeedWork;
using ATLAS.i18n.Domain.ValueObjects;

namespace ATLAS.i18n.Domain.Entities;

/// <summary>
/// Stores locale-specific formatting rules for a given language.
/// Covers date/time patterns, general number formatting, and decimal precision.
///
/// This is an owned entity within the <see cref="Language"/> aggregate.
/// </summary>
public sealed class LocaleConfiguration : Entity<Guid>, IAuditableEntity
{
    // ─── Identity ─────────────────────────────────────────────────────────────

    /// <summary>Language this configuration belongs to.</summary>
    public LanguageCode LanguageCode { get; private set; } = null!;

    // ─── Date / Time formats ─────────────────────────────────────────────────
    // Uses .NET/standard format specifiers (see DateTime.ToString docs).

    /// <summary>
    /// Full date pattern, e.g. "MM/dd/yyyy" (EN) or "dd/MM/yyyy" (ES).
    /// </summary>
    public string DateFormat { get; private set; } = null!;

    /// <summary>
    /// Short date pattern (compact view), e.g. "M/d/yy" (EN) or "d/M/yy" (ES).
    /// </summary>
    public string ShortDateFormat { get; private set; } = null!;

    /// <summary>
    /// Time-only pattern, e.g. "h:mm tt" (EN 12h) or "HH:mm" (ES 24h).
    /// </summary>
    public string TimeFormat { get; private set; } = null!;

    /// <summary>
    /// Full date+time pattern, e.g. "MM/dd/yyyy h:mm tt" (EN).
    /// </summary>
    public string DateTimeFormat { get; private set; } = null!;

    /// <summary>
    /// First day of the week for calendar display.
    /// EN → Sunday; ES/CA/FR/DE → Monday.
    /// </summary>
    public FirstDayOfWeek FirstDayOfWeek { get; private set; }

    // ─── Number formatting ────────────────────────────────────────────────────

    /// <summary>
    /// Character used as the decimal separator in general numbers.
    /// EN → '.' ; ES → ','
    /// </summary>
    public char DecimalSeparator { get; private set; }

    /// <summary>
    /// Character used as the thousands (grouping) separator.
    /// EN → ',' ; ES → '.'
    /// </summary>
    public char ThousandsSeparator { get; private set; }

    /// <summary>
    /// Default number of decimal places for non-monetary numbers.
    /// Typically 2 for most locales.
    /// </summary>
    public int DecimalPlaces { get; private set; }

    /// <summary>
    /// Number of decimal places for monetary amounts.
    /// Most currencies use 2; some (JPY, KWD) use 0 or 3.
    /// This is the locale-level default; individual <see cref="CurrencyFormat"/> entries
    /// override this per currency.
    /// </summary>
    public int CurrencyDecimalPlaces { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // EF Core constructor
    private LocaleConfiguration() { }

    private LocaleConfiguration(
        Guid id,
        LanguageCode languageCode,
        string dateFormat,
        string shortDateFormat,
        string timeFormat,
        string dateTimeFormat,
        FirstDayOfWeek firstDayOfWeek,
        char decimalSeparator,
        char thousandsSeparator,
        int decimalPlaces,
        int currencyDecimalPlaces)
    {
        Id                    = id;
        LanguageCode          = languageCode;
        DateFormat            = dateFormat;
        ShortDateFormat       = shortDateFormat;
        TimeFormat            = timeFormat;
        DateTimeFormat        = dateTimeFormat;
        FirstDayOfWeek        = firstDayOfWeek;
        DecimalSeparator      = decimalSeparator;
        ThousandsSeparator    = thousandsSeparator;
        DecimalPlaces         = decimalPlaces;
        CurrencyDecimalPlaces = currencyDecimalPlaces;
        CreatedAt             = DateTime.UtcNow;
        UpdatedAt             = DateTime.UtcNow;

        Validate();
    }

    public static LocaleConfiguration Create(
        string languageCode,
        string dateFormat,
        string shortDateFormat,
        string timeFormat,
        string dateTimeFormat,
        FirstDayOfWeek firstDayOfWeek,
        char decimalSeparator,
        char thousandsSeparator,
        int decimalPlaces = 2,
        int currencyDecimalPlaces = 2)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dateFormat);
        ArgumentException.ThrowIfNullOrWhiteSpace(shortDateFormat);
        ArgumentException.ThrowIfNullOrWhiteSpace(timeFormat);
        ArgumentException.ThrowIfNullOrWhiteSpace(dateTimeFormat);

        return new LocaleConfiguration(
            Guid.NewGuid(),
            LanguageCode.From(languageCode),
            dateFormat.Trim(),
            shortDateFormat.Trim(),
            timeFormat.Trim(),
            dateTimeFormat.Trim(),
            firstDayOfWeek,
            decimalSeparator,
            thousandsSeparator,
            decimalPlaces,
            currencyDecimalPlaces);
    }

    public void Update(
        string dateFormat,
        string shortDateFormat,
        string timeFormat,
        string dateTimeFormat,
        FirstDayOfWeek firstDayOfWeek,
        char decimalSeparator,
        char thousandsSeparator,
        int decimalPlaces,
        int currencyDecimalPlaces)
    {
        DateFormat            = dateFormat.Trim();
        ShortDateFormat       = shortDateFormat.Trim();
        TimeFormat            = timeFormat.Trim();
        DateTimeFormat        = dateTimeFormat.Trim();
        FirstDayOfWeek        = firstDayOfWeek;
        DecimalSeparator      = decimalSeparator;
        ThousandsSeparator    = thousandsSeparator;
        DecimalPlaces         = decimalPlaces;
        CurrencyDecimalPlaces = currencyDecimalPlaces;
        UpdatedAt             = DateTime.UtcNow;

        Validate();
    }

    private void Validate()
    {
        if (DecimalSeparator == ThousandsSeparator)
            throw new Exceptions.I18nDomainException(
                "DecimalSeparator and ThousandsSeparator must be different characters.");

        if (DecimalPlaces < 0 || DecimalPlaces > 10)
            throw new Exceptions.I18nDomainException(
                $"DecimalPlaces must be between 0 and 10. Got: {DecimalPlaces}.");

        if (CurrencyDecimalPlaces < 0 || CurrencyDecimalPlaces > 10)
            throw new Exceptions.I18nDomainException(
                $"CurrencyDecimalPlaces must be between 0 and 10. Got: {CurrencyDecimalPlaces}.");
    }
}
