using ATLAS.i18n.Domain.SeedWork;
using ATLAS.i18n.Domain.ValueObjects;

namespace ATLAS.i18n.Domain.Entities;

/// <summary>
/// Defines how monetary amounts are formatted for a specific currency within a given language/locale.
///
/// For example, USD in EN-US vs USD in ES-ES may differ:
///   EN-US: $1,234.56  (symbol before, period decimal, comma thousands)
///   ES-ES: 1.234,56 $ (symbol after, comma decimal, period thousands)
///
/// This is an owned entity within the <see cref="Language"/> aggregate.
/// </summary>
public sealed class CurrencyFormat : Entity<Guid>
{
    // ─── Identity ─────────────────────────────────────────────────────────────

    /// <summary>Language this format applies to.</summary>
    public LanguageCode LanguageCode { get; private set; } = null!;

    // ─── Currency identification ──────────────────────────────────────────────

    /// <summary>ISO 4217 three-letter currency code, e.g. "USD", "EUR", "GBP".</summary>
    public string CurrencyCode { get; private set; } = null!;

    /// <summary>Full name of the currency in the target language, e.g. "US Dollar", "Euro".</summary>
    public string CurrencyName { get; private set; } = null!;

    // ─── Symbol formatting ────────────────────────────────────────────────────

    /// <summary>Currency symbol, e.g. "$", "€", "£", "¥".</summary>
    public string Symbol { get; private set; } = null!;

    /// <summary>
    /// Defines where the symbol appears relative to the numeric amount.
    /// <see cref="CurrencySymbolPosition.Before"/>: $1,234.56
    /// <see cref="CurrencySymbolPosition.After"/>:  1.234,56 €
    /// </summary>
    public CurrencySymbolPosition SymbolPosition { get; private set; }

    /// <summary>
    /// Whether a space is inserted between the symbol and the number.
    /// EN: "$1,234.56" (no space) vs FR: "1 234,56 €" (space before symbol).
    /// </summary>
    public bool SpaceBetweenSymbolAndAmount { get; private set; }

    // ─── Number formatting ────────────────────────────────────────────────────

    /// <summary>Decimal separator for this currency in this language. EN → '.'; ES → ','</summary>
    public char DecimalSeparator { get; private set; }

    /// <summary>Thousands grouping separator. EN → ','; ES → '.'; FR → ' '</summary>
    public char ThousandsSeparator { get; private set; }

    /// <summary>
    /// Number of decimal places shown for this currency.
    /// USD/EUR/GBP → 2; JPY/KRW → 0; KWD/BHD → 3.
    /// </summary>
    public int DecimalPlaces { get; private set; }

    // ─── Negative amount formatting ───────────────────────────────────────────

    /// <summary>
    /// Pattern for negative amounts. Supports tokens {symbol} and {amount}.
    /// Examples:  "-{symbol}{amount}"  → -$1,234.56
    ///            "({symbol}{amount})" → ($1,234.56)  [accounting style]
    ///            "{symbol}-{amount}"  → $-1,234.56
    /// </summary>
    public string NegativePattern { get; private set; } = null!;

    // EF Core constructor
    private CurrencyFormat() { }

    private CurrencyFormat(
        Guid id,
        LanguageCode languageCode,
        string currencyCode,
        string currencyName,
        string symbol,
        CurrencySymbolPosition symbolPosition,
        bool spaceBetweenSymbolAndAmount,
        char decimalSeparator,
        char thousandsSeparator,
        int decimalPlaces,
        string negativePattern)
    {
        Id                           = id;
        LanguageCode                 = languageCode;
        CurrencyCode                 = currencyCode;
        CurrencyName                 = currencyName;
        Symbol                       = symbol;
        SymbolPosition               = symbolPosition;
        SpaceBetweenSymbolAndAmount  = spaceBetweenSymbolAndAmount;
        DecimalSeparator             = decimalSeparator;
        ThousandsSeparator           = thousandsSeparator;
        DecimalPlaces                = decimalPlaces;
        NegativePattern              = negativePattern;

        Validate();
    }

    public static CurrencyFormat Create(
        string languageCode,
        string currencyCode,
        string currencyName,
        string symbol,
        CurrencySymbolPosition symbolPosition,
        bool spaceBetweenSymbolAndAmount,
        char decimalSeparator,
        char thousandsSeparator,
        int decimalPlaces,
        string negativePattern = "-{symbol}{amount}")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(negativePattern);

        return new CurrencyFormat(
            Guid.NewGuid(),
            LanguageCode.From(languageCode),
            currencyCode.ToUpperInvariant().Trim(),
            currencyName.Trim(),
            symbol.Trim(),
            symbolPosition,
            spaceBetweenSymbolAndAmount,
            decimalSeparator,
            thousandsSeparator,
            decimalPlaces,
            negativePattern.Trim());
    }

    public void Update(
        string currencyName,
        string symbol,
        CurrencySymbolPosition symbolPosition,
        bool spaceBetweenSymbolAndAmount,
        char decimalSeparator,
        char thousandsSeparator,
        int decimalPlaces,
        string negativePattern)
    {
        CurrencyName                = currencyName.Trim();
        Symbol                      = symbol.Trim();
        SymbolPosition              = symbolPosition;
        SpaceBetweenSymbolAndAmount = spaceBetweenSymbolAndAmount;
        DecimalSeparator            = decimalSeparator;
        ThousandsSeparator          = thousandsSeparator;
        DecimalPlaces               = decimalPlaces;
        NegativePattern             = negativePattern.Trim();

        Validate();
    }

    /// <summary>
    /// Formats a decimal amount according to this currency's rules.
    /// </summary>
    public string Format(decimal amount)
    {
        var absAmount = Math.Abs(amount);
        var formatted = FormatAbsoluteAmount(absAmount);

        var space = SpaceBetweenSymbolAndAmount ? " " : string.Empty;

        string withSymbol = SymbolPosition == CurrencySymbolPosition.Before
            ? $"{Symbol}{space}{formatted}"
            : $"{formatted}{space}{Symbol}";

        if (amount < 0)
        {
            withSymbol = NegativePattern
                .Replace("{symbol}", SymbolPosition == CurrencySymbolPosition.Before ? $"{Symbol}{space}" : $"{space}{Symbol}")
                .Replace("{amount}", formatted);
        }

        return withSymbol;
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private string FormatAbsoluteAmount(decimal amount)
    {
        var rounded = Math.Round(amount, DecimalPlaces);
        var intPart = (long)Math.Truncate(rounded);
        var decPart = rounded - Math.Truncate(rounded);

        var intStr = FormatIntegerWithGrouping(intPart);

        if (DecimalPlaces == 0)
            return intStr;

        var decStr = Math.Abs(decPart)
            .ToString($"F{DecimalPlaces}")
            .Substring(2); // remove "0."

        return $"{intStr}{DecimalSeparator}{decStr}";
    }

    private string FormatIntegerWithGrouping(long value)
    {
        var str = Math.Abs(value).ToString();
        if (str.Length <= 3) return str;

        var parts = new List<string>();
        while (str.Length > 3)
        {
            parts.Insert(0, str[^3..]);
            str = str[..^3];
        }
        if (str.Length > 0) parts.Insert(0, str);

        return string.Join(ThousandsSeparator, parts);
    }

    private void Validate()
    {
        if (CurrencyCode.Length != 3 || !CurrencyCode.All(char.IsLetter))
            throw new Exceptions.I18nDomainException(
                $"CurrencyCode '{CurrencyCode}' must be a 3-letter ISO 4217 code.");

        if (DecimalSeparator == ThousandsSeparator)
            throw new Exceptions.I18nDomainException(
                "DecimalSeparator and ThousandsSeparator must be different characters.");

        if (DecimalPlaces < 0 || DecimalPlaces > 10)
            throw new Exceptions.I18nDomainException(
                $"DecimalPlaces must be between 0 and 10. Got: {DecimalPlaces}.");

        if (!NegativePattern.Contains("{amount}"))
            throw new Exceptions.I18nDomainException(
                "NegativePattern must contain the {amount} placeholder.");
    }
}
