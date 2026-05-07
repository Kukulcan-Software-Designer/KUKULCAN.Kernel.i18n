namespace ATLAS.Kernel.i18n.Application.Contracts.Requests;

/// <summary>
/// Represents the UpsertCurrencyRequest record.
/// </summary>
/// <param name="CurrencyName">The CurrencyName parameter.</param>
/// <param name="Symbol">The Symbol parameter.</param>
/// <param name="SymbolPosition">The SymbolPosition parameter.</param>
/// <param name="SpaceBetweenSymbolAndAmount">The SpaceBetweenSymbolAndAmount parameter.</param>
/// <param name="DecimalSeparator">The DecimalSeparator parameter.</param>
/// <param name="ThousandsSeparator">The ThousandsSeparator parameter.</param>
/// <param name="DecimalPlaces">The DecimalPlaces parameter.</param>
/// <param name="NegativePattern">The NegativePattern parameter.</param>
public record UpsertCurrencyRequest(string CurrencyName, string Symbol, string SymbolPosition, bool SpaceBetweenSymbolAndAmount,
    string DecimalSeparator, string ThousandsSeparator, int DecimalPlaces, string NegativePattern = "-{symbol}{amount}");
