using ATLAS.Kernel.i18n.Domain.ValueObjects.Enums;

namespace ATLAS.Kernel.i18n.Domain.Entities;

/// <summary>
/// Defines how monetary amounts are formatted for a specific currency within a given language.
///
/// <para>
/// The same ISO 4217 currency can have different visual representations per language:
/// <list type="bullet">
///   <item>USD in <c>en-US</c>: <c>$1,234.56</c> (symbol before, comma thousands, period decimal)</item>
///   <item>USD in <c>es-ES</c>: <c>1.234,56 $</c> (symbol after with space, period thousands, comma decimal)</item>
///   <item>EUR in <c>fr-FR</c>: <c>1 234,56 €</c> (symbol after with space, narrow-space thousands)</item>
/// </list>
/// </para>
///
/// <para>
/// Extends <see cref="AuditableEntityBase{TId}"/> — global (not tenant-scoped),
/// audit-tracked, never soft-deleted.
/// </para>
/// </summary>
public sealed class CurrencyFormat : AuditableEntityBase<Guid>
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>BCP-47 code of the language this format applies to (e.g. <c>es-ES</c>).</summary>
    public LanguageCode LanguageCode { get; private set; } = null!;

    // ── Currency identification ───────────────────────────────────────────────

    /// <summary>ISO 4217 three-letter currency code (e.g. <c>"USD"</c>, <c>"EUR"</c>, <c>"GBP"</c>).</summary>
    public string CurrencyCode { get; private set; } = string.Empty;

    /// <summary>Full currency name in the target language (e.g. <c>"Dólar estadounidense"</c>).</summary>
    public string CurrencyName { get; private set; } = string.Empty;

    // ── Symbol formatting ─────────────────────────────────────────────────────

    /// <summary>Currency symbol: <c>"$"</c>, <c>"€"</c>, <c>"£"</c>, <c>"¥"</c>…</summary>
    public string Symbol { get; private set; } = string.Empty;

    /// <summary>
    /// Where the symbol is placed relative to the amount.
    /// <see cref="CurrencySymbolPosition.Before"/>: <c>$1,234.56</c>
    /// <see cref="CurrencySymbolPosition.After"/>:  <c>1.234,56 €</c>
    /// </summary>
    public CurrencySymbolPosition SymbolPosition { get; private set; }

    /// <summary>
    /// <c>true</c> when a space separates the symbol from the number
    /// (e.g. French: <c>1 234,56 €</c>; English: <c>$1,234.56</c> → no space).
    /// </summary>
    public bool SpaceBetweenSymbolAndAmount { get; private set; }

    // ── Number formatting ─────────────────────────────────────────────────────

    /// <summary>Decimal separator for this currency in this language.</summary>
    public char DecimalSeparator { get; private set; }

    /// <summary>Thousands grouping separator for this currency in this language.</summary>
    public char ThousandsSeparator { get; private set; }

    /// <summary>
    /// Decimal places shown for this currency.
    /// USD/EUR/GBP → 2; JPY/KRW → 0; KWD/BHD → 3.
    /// </summary>
    public int DecimalPlaces { get; private set; }

    // ── Negative amount pattern ───────────────────────────────────────────────

    /// <summary>
    /// Pattern for negative amounts. Tokens: <c>{symbol}</c> and <c>{amount}</c>.
    /// Examples:
    /// <list type="bullet">
    ///   <item><c>"-{symbol}{amount}"</c>  → <c>-$1,234.56</c></item>
    ///   <item><c>"({symbol}{amount})"</c> → <c>($1,234.56)</c> (accounting style)</item>
    ///   <item><c>"-{amount} {symbol}"</c> → <c>-1.234,56 €</c></item>
    /// </list>
    /// </summary>
    public string NegativePattern { get; private set; } = string.Empty;

    // ── EF Core constructor ───────────────────────────────────────────────────

    // ReSharper disable once UnusedMember.Local
    private CurrencyFormat() { }

    // ── Factory method ────────────────────────────────────────────────────────

    /// <summary>Creates a new <see cref="CurrencyFormat"/> entry after full validation.</summary>
    public static Result<CurrencyFormat> Create(Guid id, string languageCode, string currencyCode, string currencyName, string symbol,
        CurrencySymbolPosition symbolPosition, bool spaceBetweenSymbolAndAmount, char decimalSeparator, char thousandsSeparator,
        int decimalPlaces, string negativePattern = "-{symbol}{amount}")
    {
        var langResult = LanguageCode.Create(languageCode);
        if (langResult.IsFailure) return langResult.Error;

        if (string.IsNullOrWhiteSpace(currencyCode) ||
            currencyCode.Trim().Length != 3 ||
            !currencyCode.Trim().All(char.IsLetter))
            return Error.Validation(
                "CurrencyFormat.CurrencyCode.Invalid",
                $"'{currencyCode}' is not a valid ISO 4217 three-letter currency code.");

        if (string.IsNullOrWhiteSpace(currencyName))
            return Error.Validation("CurrencyFormat.CurrencyName.Empty", "Currency name must not be empty.");

        if (string.IsNullOrWhiteSpace(symbol))
            return Error.Validation("CurrencyFormat.Symbol.Empty", "Currency symbol must not be empty.");

        if (decimalSeparator == thousandsSeparator)
            return Error.Validation(
                "CurrencyFormat.Separators.Conflict",
                "DecimalSeparator and ThousandsSeparator must be different characters.");

        if (decimalPlaces is < 0 or > 10)
            return Error.Validation(
                "CurrencyFormat.DecimalPlaces.OutOfRange",
                $"DecimalPlaces must be between 0 and 10. Got: {decimalPlaces}.");

        if (string.IsNullOrWhiteSpace(negativePattern) || !negativePattern.Contains("{amount}"))
            return Error.Validation(
                "CurrencyFormat.NegativePattern.Invalid",
                "NegativePattern must contain the {amount} placeholder.");

        return new CurrencyFormat
        {
            Id                          = Guard.Against.Default(id, nameof(id)),
            LanguageCode                = langResult.Value,
            CurrencyCode                = currencyCode.Trim().ToUpperInvariant(),
            CurrencyName                = currencyName.Trim(),
            Symbol                      = symbol.Trim(),
            SymbolPosition              = symbolPosition,
            SpaceBetweenSymbolAndAmount = spaceBetweenSymbolAndAmount,
            DecimalSeparator            = decimalSeparator,
            ThousandsSeparator          = thousandsSeparator,
            DecimalPlaces               = decimalPlaces,
            NegativePattern             = negativePattern.Trim(),
        };
    }

    // ── Business method ───────────────────────────────────────────────────────

    /// <summary>Replaces all formatting properties of this currency format entry.</summary>
    public Result Update(string currencyName, string symbol, CurrencySymbolPosition symbolPosition, bool spaceBetweenSymbolAndAmount,
        char decimalSeparator, char thousandsSeparator, int decimalPlaces, string negativePattern)
    {
        if (decimalSeparator == thousandsSeparator)
            return Error.Validation(
                "CurrencyFormat.Separators.Conflict",
                "DecimalSeparator and ThousandsSeparator must be different characters.");

        if (decimalPlaces is < 0 or > 10)
            return Error.Validation(
                "CurrencyFormat.DecimalPlaces.OutOfRange",
                $"DecimalPlaces must be between 0 and 10. Got: {decimalPlaces}.");

        if (string.IsNullOrWhiteSpace(negativePattern) || !negativePattern.Contains("{amount}"))
            return Error.Validation(
                "CurrencyFormat.NegativePattern.Invalid",
                "NegativePattern must contain the {amount} placeholder.");

        CurrencyName                = Guard.Against.NullOrWhiteSpace(currencyName, nameof(currencyName)).Trim();
        Symbol                      = Guard.Against.NullOrWhiteSpace(symbol, nameof(symbol)).Trim();
        SymbolPosition              = symbolPosition;
        SpaceBetweenSymbolAndAmount = spaceBetweenSymbolAndAmount;
        DecimalSeparator            = decimalSeparator;
        ThousandsSeparator          = thousandsSeparator;
        DecimalPlaces               = decimalPlaces;
        NegativePattern             = negativePattern.Trim();
        return Result.Ok();
    }

    // ── Domain helper ─────────────────────────────────────────────────────────

    /// <summary>
    /// Formats a <paramref name="amount"/> according to this currency's rules.
    /// Returns a display string such as <c>1.234,56 €</c> or <c>$1,234.56</c>.
    /// </summary>
    public string Format(decimal amount)
    {
        var isNegative = amount < 0;
        var absStr     = FormatAbsoluteAmount(Math.Abs(amount));
        var space      = SpaceBetweenSymbolAndAmount ? " " : string.Empty;

        var withSymbol = SymbolPosition == CurrencySymbolPosition.Before
            ? $"{Symbol}{space}{absStr}"
            : $"{absStr}{space}{Symbol}";

        if (!isNegative) return withSymbol;

        return NegativePattern
            .Replace("{symbol}", SymbolPosition == CurrencySymbolPosition.Before
                ? $"{Symbol}{space}"
                : $"{space}{Symbol}")
            .Replace("{amount}", absStr);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private string FormatAbsoluteAmount(decimal value)
    {
        var rounded  = Math.Round(value, DecimalPlaces);
        var intPart  = (long)Math.Truncate(rounded);
        var decPart  = rounded - Math.Truncate(rounded);
        var intStr   = FormatIntegerWithGrouping(intPart);

        if (DecimalPlaces == 0) return intStr;

        var decStr = Math.Abs(decPart).ToString($"F{DecimalPlaces}")[2..]; // remove "0."
        return $"{intStr}{DecimalSeparator}{decStr}";
    }

    private string FormatIntegerWithGrouping(long value)
    {
        var str   = Math.Abs(value).ToString();
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
}
