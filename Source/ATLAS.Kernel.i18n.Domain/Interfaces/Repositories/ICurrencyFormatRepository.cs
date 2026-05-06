namespace ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;

/// <summary>
/// Write repository for <see cref="CurrencyFormat"/>.
/// </summary>
public interface ICurrencyFormatRepository : IRepository<CurrencyFormat, Guid>
{
    /// <summary>Finds a currency format for a language + ISO 4217 code pair, or <c>null</c>.</summary>
    Task<CurrencyFormat?> FindAsync(LanguageCode languageCode, string currencyCode, CancellationToken ct = default);

    /// <summary>Returns all currency formats configured for a given language.</summary>
    Task<IReadOnlyList<CurrencyFormat>> GetByLanguageAsync(LanguageCode languageCode, CancellationToken ct = default);

    /// <summary>Removes a currency format entry from the database.</summary>
    void Remove(CurrencyFormat currencyFormat);
}
