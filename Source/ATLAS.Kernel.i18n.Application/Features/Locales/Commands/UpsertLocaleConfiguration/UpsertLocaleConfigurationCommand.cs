using ATLAS.Kernel.i18n.Domain.DTOs;

namespace ATLAS.Kernel.i18n.Application.Features.Locales.Commands.UpsertLocaleConfiguration;

/// <summary>
/// Representa un comando para crear o actualizar la configuración regional, incluyendo formatos de fecha, hora y
/// separadores numéricos.
/// </summary>
/// <remarks>Utilice este comando para establecer o modificar la configuración regional de una aplicación,
/// asegurando que los formatos y separadores coincidan con las convenciones culturales del usuario. Los valores
/// predeterminados para los decimales son 2.</remarks>
/// <param name="LanguageCode">El código de idioma que identifica la configuración regional, por ejemplo, "es-ES" o "en-US". No puede ser nulo ni
/// estar vacío.</param>
/// <param name="DateFormat">El formato de fecha largo utilizado en la configuración regional, por ejemplo, "dd/MM/yyyy".</param>
/// <param name="ShortDateFormat">El formato de fecha corto utilizado en la configuración regional, por ejemplo, "dd/MM/yy".</param>
/// <param name="TimeFormat">El formato de hora utilizado en la configuración regional, por ejemplo, "HH:mm:ss".</param>
/// <param name="DateTimeFormat">El formato de fecha y hora combinado utilizado en la configuración regional.</param>
/// <param name="FirstDayOfWeek">El primer día de la semana según la configuración regional, por ejemplo, "Monday" o "Sunday".</param>
/// <param name="DecimalSeparator">El carácter utilizado como separador decimal, por ejemplo, "," o ".".</param>
/// <param name="ThousandsSeparator">El carácter utilizado como separador de miles, por ejemplo, "." o ",".</param>
/// <param name="DecimalPlaces">El número de decimales que se deben mostrar para los valores numéricos. Debe ser mayor o igual a cero.</param>
/// <param name="CurrencyDecimalPlaces">El número de decimales que se deben mostrar para los valores monetarios. Debe ser mayor o igual a cero.</param>
public record UpsertLocaleConfigurationCommand(
    string LanguageCode,
    string DateFormat,
    string ShortDateFormat,
    string TimeFormat,
    string DateTimeFormat,
    string FirstDayOfWeek,
    string DecimalSeparator,
    string ThousandsSeparator,
    int DecimalPlaces = 2,
    int CurrencyDecimalPlaces = 2)
    : IRequest<Result<LocaleConfigurationDto>>;
