namespace KUKULCAN.Kernel.i18n.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ILanguageRepository"/>.
/// Base CRUD operations are provided by the repository directly via DbContext —
/// the SharedKernel defines the interface, not a generic base repository class.
/// </summary>
/// <param name="context">The database context.</param>
public sealed class LanguageRepository(I18NDbContext context) : ILanguageRepository
{
    /// <summary>
    /// Gets a language by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the language.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The language if found; otherwise, null.</returns>
    public async Task<Language?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Languages
            .Include(l => l.LocaleConfiguration)
            .Include(l => l.CurrencyFormats)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

    /// <summary>
    /// Gets a language by its BCP-47 code.
    /// </summary>
    /// <param name="bcp47Code">The BCP-47 code of the language.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The language if found; otherwise, null.</returns>
    public async Task<Language?> GetByCodeAsync(string bcp47Code, CancellationToken ct = default) =>
        await context.Languages
            .Include(l => l.LocaleConfiguration)
            .Include(l => l.CurrencyFormats)
            .FirstOrDefaultAsync(l => l.Code == bcp47Code, ct);

    /// <summary>
    /// Lists all languages.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A list of all languages.</returns>
    public async Task<IReadOnlyList<Language>> ListAllAsync(CancellationToken ct = default) =>
        await context.Languages
            .OrderBy(l => l.Name)
            .ToListAsync(ct);

    /// <summary>
    /// Gets all active languages.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A list of all active languages.</returns>
    public async Task<IReadOnlyList<Language>> GetAllActiveAsync(CancellationToken ct = default) =>
        await context.Languages
            .Where(l => l.IsActive)
            .OrderBy(l => l.Name)
            .ToListAsync(ct);

    /// <summary>
    /// Gets the default language.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The default language if found; otherwise, null.</returns>
    public async Task<Language?> GetDefaultAsync(CancellationToken ct = default) => await context.Languages.FirstOrDefaultAsync(l => l.IsDefault, ct);

    /// <summary>
    /// Checks if a language exists by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the language.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True if the language exists; otherwise, false.</returns>
    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) => await context.Languages.AnyAsync(l => l.Id == id, ct);

    /// <summary>
    /// Checks if a language exists by its BCP-47 code.
    /// </summary>
    /// <param name="bcp47Code">The BCP-47 code of the language.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True if the language exists; otherwise, false.</returns>
    public async Task<bool> ExistsByCodeAsync(string bcp47Code, CancellationToken ct = default) => await context.Languages.AnyAsync(l => l.Code == bcp47Code, ct);

    /// <summary>
    /// Adds a new language.
    /// </summary>
    /// <param name="language">The language to add.</param>
    /// <param name="ct">The cancellation token.</param>
    public async Task AddAsync(Language language, CancellationToken ct = default) => await context.Languages.AddAsync(language, ct);

    /// <summary>
    /// Updates an existing language.
    /// </summary>
    /// <param name="language">The language to update.</param>
    public void Update(Language language) => context.Languages.Update(language);
}
