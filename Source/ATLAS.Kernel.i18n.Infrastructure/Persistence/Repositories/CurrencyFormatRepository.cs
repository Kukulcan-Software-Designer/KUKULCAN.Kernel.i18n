namespace ATLAS.Kernel.i18n.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for managing currency formats.
/// </summary>
/// <param name="context">The database context.</param>
public sealed class CurrencyFormatRepository(I18NDbContext context) : ICurrencyFormatRepository
{
    /// <summary>
    /// Gets a currency format by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the currency format.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The currency format if found; otherwise, null.</returns>
    public async Task<CurrencyFormat?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.CurrencyFormats.FirstOrDefaultAsync(cf => cf.Id == id, ct);

    /// <summary>
    /// Finds a currency format by its language code and currency code.
    /// </summary>
    /// <param name="languageCode">The language code of the currency format.</param>
    /// <param name="currencyCode">The currency code of the currency format.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The currency format if found; otherwise, null.</returns>
    public async Task<CurrencyFormat?> FindAsync(LanguageCode languageCode, string currencyCode, CancellationToken ct = default) =>
        await context.CurrencyFormats
            .FirstOrDefaultAsync(cf =>
                cf.LanguageCode == languageCode &&
                cf.CurrencyCode.Equals(currencyCode, StringComparison.InvariantCultureIgnoreCase), ct);

    /// <summary>
    /// Lists all currency formats.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A list of all currency formats.</returns>
    public async Task<IReadOnlyList<CurrencyFormat>> ListAllAsync(CancellationToken ct = default) =>
        await context.CurrencyFormats
            .OrderBy(cf => cf.LanguageCode)
            .ThenBy(cf => cf.CurrencyCode)
            .ToListAsync(ct);

    /// <summary>
    /// Gets all currency formats for a specific language.
    /// </summary>
    /// <param name="languageCode">The language code of the currency formats.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A list of currency formats for the specified language.</returns>
    public async Task<IReadOnlyList<CurrencyFormat>> GetByLanguageAsync(LanguageCode languageCode, CancellationToken ct = default) =>
        await context.CurrencyFormats
            .Where(cf => cf.LanguageCode == languageCode)
            .OrderBy(cf => cf.CurrencyCode)
            .ToListAsync(ct);

    /// <summary>
    /// Checks if a currency format exists by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the currency format.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True if the currency format exists; otherwise, false.</returns>
    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
        await context.CurrencyFormats.AnyAsync(cf => cf.Id == id, ct);

    /// <summary>
    /// Adds a new currency format.
    /// </summary>
    /// <param name="format">The currency format to add.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AddAsync(CurrencyFormat format, CancellationToken ct = default) =>
        await context.CurrencyFormats.AddAsync(format, ct);

    /// <summary>
    /// Updates an existing currency format.
    /// </summary>
    /// <param name="format">The currency format to update.</param>
    public void Update(CurrencyFormat format) => context.CurrencyFormats.Update(format);

    /// <summary>
    /// Removes a currency format.
    /// </summary>
    /// <param name="format">The format parameter.</param>
    public void Remove(CurrencyFormat format) => context.CurrencyFormats.Remove(format);
}
