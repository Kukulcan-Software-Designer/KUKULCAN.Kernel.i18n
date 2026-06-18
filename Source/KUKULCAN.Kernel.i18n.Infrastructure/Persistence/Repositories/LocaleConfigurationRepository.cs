namespace KUKULCAN.Kernel.i18n.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ILocaleConfigurationRepository"/>.
/// Base CRUD operations are provided by the repository directly via DbContext —
/// the SharedKernel defines the interface, not a generic base repository class.
/// </summary>
/// <param name="context">The database context.</param>
public sealed class LocaleConfigurationRepository(I18NDbContext context) : ILocaleConfigurationRepository
{
    /// <summary>
    /// Gets a locale configuration by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the locale configuration.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The locale configuration if found; otherwise, null.</returns>
    public async Task<LocaleConfiguration?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.LocaleConfigurations.FirstOrDefaultAsync(lc => lc.Id == id, ct);

    /// <summary>
    /// Gets a locale configuration by its language code.
    /// </summary>
    /// <param name="languageCode">The language code of the locale configuration.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The locale configuration if found; otherwise, null.</returns>
    public async Task<LocaleConfiguration?> GetByLanguageAsync(LanguageCode languageCode, CancellationToken ct = default) =>
        await context.LocaleConfigurations.FirstOrDefaultAsync(lc => lc.LanguageCode == languageCode, ct);

    /// <summary>
    /// Lists all locale configurations.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A list of all locale configurations.</returns>
    public async Task<IReadOnlyList<LocaleConfiguration>> ListAllAsync(CancellationToken ct = default) =>
        await context.LocaleConfigurations.OrderBy(lc => lc.LanguageCode).ToListAsync(ct);

    /// <summary>
    /// Gets all locale configurations.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A list of all locale configurations.</returns>
    public async Task<IReadOnlyList<LocaleConfiguration>> GetAllAsync(CancellationToken ct = default) => await ListAllAsync(ct);

    /// <summary>
    /// Checks if a locale configuration exists by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the locale configuration.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True if the locale configuration exists; otherwise, false.</returns>
    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) => await context.LocaleConfigurations.AnyAsync(lc => lc.Id == id, ct);

    /// <summary>
    /// Adds a new locale configuration.
    /// </summary>
    /// <param name="config">The locale configuration to add.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AddAsync(LocaleConfiguration config, CancellationToken ct = default) => await context.LocaleConfigurations.AddAsync(config, ct);

    /// <summary>
    /// Updates an existing locale configuration.
    /// </summary>
    /// <param name="config">The locale configuration to update.</param>
    public void Update(LocaleConfiguration config) => context.LocaleConfigurations.Update(config);
}
