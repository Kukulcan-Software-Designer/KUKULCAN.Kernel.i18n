namespace ATLAS.Kernel.i18n.Application.Contracts.Requests;

/// <summary>
/// 
/// </summary>
/// <param name="CurrencyName"></param>
/// <param name="Symbol"></param>
/// <param name="SymbolPosition"></param>
/// <param name="SpaceBetweenSymbolAndAmount"></param>
/// <param name="DecimalSeparator"></param>
/// <param name="ThousandsSeparator"></param>
/// <param name="DecimalPlaces"></param>
/// <param name="NegativePattern"></param>
public record UpsertCurrencyRequest(string CurrencyName, string Symbol, string SymbolPosition, bool SpaceBetweenSymbolAndAmount,
    string DecimalSeparator, string ThousandsSeparator, int DecimalPlaces, string NegativePattern = "-{symbol}{amount}");
