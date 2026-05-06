namespace ATLAS.Kernel.i18n.Application.Contracts.Requests;

/// <summary>
/// 
/// </summary>
/// <param name="DateFormat"></param>
/// <param name="ShortDateFormat"></param>
/// <param name="TimeFormat"></param>
/// <param name="DateTimeFormat"></param>
/// <param name="FirstDayOfWeek"></param>
/// <param name="DecimalSeparator"></param>
/// <param name="ThousandsSeparator"></param>
/// <param name="DecimalPlaces"></param>
/// <param name="CurrencyDecimalPlaces"></param>
public record UpsertLocaleRequest(
    string DateFormat, string ShortDateFormat, string TimeFormat,
    string DateTimeFormat, string FirstDayOfWeek,
    string DecimalSeparator, string ThousandsSeparator,
    int DecimalPlaces = 2, int CurrencyDecimalPlaces = 2);
