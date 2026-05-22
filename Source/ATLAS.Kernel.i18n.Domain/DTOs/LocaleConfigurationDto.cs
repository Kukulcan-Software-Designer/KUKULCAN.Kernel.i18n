namespace ATLAS.Kernel.i18n.Domain.DTOs;

/// <summary>
/// Provides functionality for this member.
/// </summary>
/// <param name="LanguageCode">The LanguageCode parameter.</param>
/// <param name="DateFormat">The DateFormat parameter.</param>
/// <param name="ShortDateFormat">The ShortDateFormat parameter.</param>
/// <param name="TimeFormat">The TimeFormat parameter.</param>
/// <param name="DateTimeFormat">The DateTimeFormat parameter.</param>
/// <param name="FirstDayOfWeek">The FirstDayOfWeek parameter.</param>
/// <param name="DecimalSeparator">The DecimalSeparator parameter.</param>
/// <param name="ThousandsSeparator">The ThousandsSeparator parameter.</param>
/// <param name="DecimalPlaces">The DecimalPlaces parameter.</param>
/// <param name="CurrencyDecimalPlaces">The CurrencyDecimalPlaces parameter.</param>
/// <param name="CreatedAt">The CreatedAt parameter.</param>
/// <param name="UpdatedAt">The UpdatedAt parameter.</param>
public record LocaleConfigurationDto(string LanguageCode, string DateFormat, string ShortDateFormat, string TimeFormat, string DateTimeFormat, string FirstDayOfWeek,
    string DecimalSeparator, string ThousandsSeparator, int DecimalPlaces, int CurrencyDecimalPlaces, DateTimeOffset CreatedAt,DateTimeOffset? UpdatedAt);
