namespace ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;

/// <summary>
/// Write repository for <see cref="LocaleConfiguration"/>.
/// </summary>
public interface ILocaleConfigurationRepository : IRepository<LocaleConfiguration, Guid>
{
    /// <summary>
    /// Returns the locale configuration for the given language, or <c>null</c>.
    /// </summary>
    /// <param name="languageCode">The language code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The locale configuration if found; otherwise, <c>null</c>.</returns>
    Task<LocaleConfiguration?> GetByLanguageAsync(LanguageCode languageCode,CancellationToken ct = default);

    /// <summary>
    /// Returns all locale configurations.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of all locale configurations.</returns>
    Task<IReadOnlyList<LocaleConfiguration>> GetAllAsync(CancellationToken ct = default);
}
