namespace ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;

/// <summary>
/// Write repository for <see cref="LocaleConfiguration"/>.
/// </summary>
public interface ILocaleConfigurationRepository : IRepository<LocaleConfiguration, Guid>
{
    /// <summary>Returns the locale configuration for the given language, or <c>null</c>.</summary>
    Task<LocaleConfiguration?> GetByLanguageAsync(LanguageCode languageCode, CancellationToken ct = default);

    /// <summary>Returns all locale configurations.</summary>
    Task<IReadOnlyList<LocaleConfiguration>> GetAllAsync(CancellationToken ct = default);
}
