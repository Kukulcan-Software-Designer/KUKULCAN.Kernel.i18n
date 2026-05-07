namespace ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;

/// <summary>
/// Write repository for <see cref="CurrencyFormat"/>.
/// </summary>
public interface ICurrencyFormatRepository : IRepository<CurrencyFormat, Guid>
{
    /// <summary>
    /// Finds a currency format for a language + ISO 4217 code pair, or <c>null</c>.
    /// </summary>
    /// <param name="languageCode">The language code.</param>
    /// <param name="currencyCode">The ISO 4217 currency code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The currency format if found; otherwise, <c>null</c>.</returns>
    Task<CurrencyFormat?> FindAsync(LanguageCode languageCode, string currencyCode, CancellationToken ct = default);

    /// <summary>
    /// Returns all currency formats configured for a given language.
    /// </summary>
    /// <param name="languageCode">The language code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of currency formats for the specified language.</returns>
    Task<IReadOnlyList<CurrencyFormat>> GetByLanguageAsync(LanguageCode languageCode, CancellationToken ct = default);

    /// <summary>
    /// Removes a currency format entry from the database.
    /// </summary>
    /// <param name="currencyFormat">The currency format to remove.</param>
    void Remove(CurrencyFormat currencyFormat);
}
