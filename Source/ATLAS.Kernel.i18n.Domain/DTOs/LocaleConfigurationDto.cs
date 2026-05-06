namespace ATLAS.Kernel.i18n.Domain.DTOs;

/// <summary>
/// 
/// </summary>
/// <param name="LanguageCode"></param>
/// <param name="DateFormat"></param>
/// <param name="ShortDateFormat"></param>
/// <param name="TimeFormat"></param>
/// <param name="DateTimeFormat"></param>
/// <param name="FirstDayOfWeek"></param>
/// <param name="DecimalSeparator"></param>
/// <param name="ThousandsSeparator"></param>
/// <param name="DecimalPlaces"></param>
/// <param name="CurrencyDecimalPlaces"></param>
/// <param name="CreatedAt"></param>
/// <param name="UpdatedAt"></param>
public record LocaleConfigurationDto(
    string LanguageCode,
    string DateFormat,
    string ShortDateFormat,
    string TimeFormat,
    string DateTimeFormat,
    string FirstDayOfWeek,
    string DecimalSeparator,
    string ThousandsSeparator,
    int DecimalPlaces,
    int CurrencyDecimalPlaces,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
