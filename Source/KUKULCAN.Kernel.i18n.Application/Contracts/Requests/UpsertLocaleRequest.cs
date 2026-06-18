namespace KUKULCAN.Kernel.i18n.Application.Contracts.Requests;

/// <summary>
/// Provides functionality for this member.
/// </summary>
/// <param name="DateFormat">The DateFormat parameter.</param>
/// <param name="ShortDateFormat">The ShortDateFormat parameter.</param>
/// <param name="TimeFormat">The TimeFormat parameter.</param>
/// <param name="DateTimeFormat">The DateTimeFormat parameter.</param>
/// <param name="FirstDayOfWeek">The FirstDayOfWeek parameter.</param>
/// <param name="DecimalSeparator">The DecimalSeparator parameter.</param>
/// <param name="ThousandsSeparator">The ThousandsSeparator parameter.</param>
/// <param name="DecimalPlaces">The DecimalPlaces parameter.</param>
/// <param name="CurrencyDecimalPlaces">The CurrencyDecimalPlaces parameter.</param>
public record UpsertLocaleRequest(
    string DateFormat, string ShortDateFormat, string TimeFormat,
    string DateTimeFormat, string FirstDayOfWeek,
    string DecimalSeparator, string ThousandsSeparator,
    int DecimalPlaces = 2, int CurrencyDecimalPlaces = 2);
