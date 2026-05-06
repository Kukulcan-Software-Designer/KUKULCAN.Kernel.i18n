namespace ATLAS.Kernel.i18n.Domain.DTOs;

/// <summary>
/// 
/// </summary>
/// <param name="Id"></param>
/// <param name="LanguageCode"></param>
/// <param name="CurrencyCode"></param>
/// <param name="CurrencyName"></param>
/// <param name="Symbol"></param>
/// <param name="SymbolPosition"></param>
/// <param name="SpaceBetweenSymbolAndAmount"></param>
/// <param name="DecimalSeparator"></param>
/// <param name="ThousandsSeparator"></param>
/// <param name="DecimalPlaces"></param>
/// <param name="NegativePattern"></param>
/// <param name="FormattedExample">>Pre-formatted example: <c>"1.234,56 €"</c> (using 1 234.56).</param>
/// <param name="CreatedAt"></param>
/// <param name="UpdatedAt"></param>
public record CurrencyFormatDto(
    Guid Id,
    string LanguageCode,
    string CurrencyCode,
    string CurrencyName,
    string Symbol,
    string SymbolPosition,
    bool SpaceBetweenSymbolAndAmount,
    string DecimalSeparator,
    string ThousandsSeparator,
    int DecimalPlaces,
    string NegativePattern,
    string FormattedExample,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
