using ATLAS.Kernel.i18n.Domain.DTOs;

namespace ATLAS.Kernel.i18n.Application.Features.Currencies.Commands.UpsertCurrencyFormat;

/// <summary>
/// Representa un comando para crear o actualizar la configuración de formato de moneda para un idioma y código de
/// moneda específicos.
/// </summary>
/// <remarks>Utilice este comando para definir o modificar cómo se muestran los importes monetarios en función de
/// las convenciones culturales y de moneda. El formato afecta la presentación de los valores en la interfaz de usuario
/// y en los informes.</remarks>
/// <param name="LanguageCode">El código de idioma ISO 639-1 que identifica el idioma para el que se aplica el formato de moneda. No puede ser nulo
/// ni estar vacío.</param>
/// <param name="CurrencyCode">El código de moneda ISO 4217 que identifica la moneda a la que se aplica el formato. No puede ser nulo ni estar
/// vacío.</param>
/// <param name="CurrencyName">El nombre descriptivo de la moneda, como se mostrará a los usuarios. No puede ser nulo ni estar vacío.</param>
/// <param name="Symbol">El símbolo que representa la moneda, como '$' o '€'.</param>
/// <param name="SymbolPosition">La posición del símbolo de la moneda en relación con el importe. Por ejemplo, 'antes' o 'después'.</param>
/// <param name="SpaceBetweenSymbolAndAmount">Indica si se debe incluir un espacio entre el símbolo de la moneda y el importe. Es <see langword="true"/> para
/// incluir un espacio; de lo contrario, <see langword="false"/>.</param>
/// <param name="DecimalSeparator">El carácter utilizado como separador decimal en los importes, como ',' o '.'.</param>
/// <param name="ThousandsSeparator">El carácter utilizado como separador de miles en los importes, como ',' o '.'.</param>
/// <param name="DecimalPlaces">El número de decimales que se mostrarán en los importes. Debe ser mayor o igual que cero.</param>
/// <param name="NegativePattern">El patrón de formato utilizado para mostrar importes negativos. Puede incluir los marcadores '{symbol}' y
/// '{amount}'. El valor predeterminado es "-{symbol}{amount}".</param>
public record UpsertCurrencyFormatCommand(string LanguageCode, string CurrencyCode, string CurrencyName, string Symbol, string SymbolPosition, bool SpaceBetweenSymbolAndAmount,
    string DecimalSeparator, string ThousandsSeparator, int DecimalPlaces, string NegativePattern = "-{symbol}{amount}") : IRequest<Result<CurrencyFormatDto>>;
