namespace KUKULCAN.Kernel.i18n.Domain.DTOs;

/// <summary>
/// Provides functionality for this member.
/// </summary>
/// <param name="Id">The Id parameter.</param>
/// <param name="LanguageCode">The LanguageCode parameter.</param>
/// <param name="CurrencyCode">The CurrencyCode parameter.</param>
/// <param name="CurrencyName">The CurrencyName parameter.</param>
/// <param name="Symbol">The Symbol parameter.</param>
/// <param name="SymbolPosition">The SymbolPosition parameter.</param>
/// <param name="SpaceBetweenSymbolAndAmount">The SpaceBetweenSymbolAndAmount parameter.</param>
/// <param name="DecimalSeparator">The DecimalSeparator parameter.</param>
/// <param name="ThousandsSeparator">The ThousandsSeparator parameter.</param>
/// <param name="DecimalPlaces">The DecimalPlaces parameter.</param>
/// <param name="NegativePattern">The NegativePattern parameter.</param>
/// <param name="FormattedExample">>Pre-formatted example: <c>"1.234,56 €"</c> (using 1 234.56).</param>
/// <param name="CreatedAt">The CreatedAt parameter.</param>
/// <param name="UpdatedAt">The UpdatedAt parameter.</param>
public record CurrencyFormatDto(Guid Id, string LanguageCode, string CurrencyCode, string CurrencyName, string Symbol, string SymbolPosition,
    bool SpaceBetweenSymbolAndAmount, string DecimalSeparator, string ThousandsSeparator, int DecimalPlaces, string NegativePattern,
    string FormattedExample, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);
