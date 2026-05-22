namespace ATLAS.Kernel.i18n.Application.Features.Currencies.Commands.DeleteCurrencyFormat;

/// <summary>
/// Represents a request to delete a currency format for a specific language and currency code.
/// </summary>
/// <param name="LanguageCode">The language code associated with the currency format to delete. Must be a valid ISO language code.</param>
/// <param name="CurrencyCode">The currency code of the format to delete. Must be a valid ISO 4217 currency code.</param>
public record DeleteCurrencyFormatCommand(string LanguageCode, string CurrencyCode) : IRequest<Result>;
