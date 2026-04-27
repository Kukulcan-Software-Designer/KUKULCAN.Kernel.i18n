namespace ATLAS.i18n.Domain.Repositories;

// ─── Language ─────────────────────────────────────────────────────────────────

/// <summary>
/// Write repository for <see cref="Language"/>.
/// Extends <see cref="IRepository{T,TId}"/> from <c>Atlas.SharedKernel.Abstractions</c>,
/// which provides <c>GetByIdAsync</c>, <c>ListAllAsync</c>, <c>AddAsync</c>,
/// <c>Update</c>, and <c>ExistsAsync</c>.
/// </summary>
public interface ILanguageRepository : IRepository<Language, Guid>
{
    /// <summary>Returns the language with the given BCP-47 code, or <c>null</c>.</summary>
    Task<Language?> GetByCodeAsync(string bcp47Code, CancellationToken ct = default);

    /// <summary>Returns all active languages ordered by display name.</summary>
    Task<IReadOnlyList<Language>> GetAllActiveAsync(CancellationToken ct = default);

    /// <summary>Returns the language currently marked as the platform default.</summary>
    Task<Language?> GetDefaultAsync(CancellationToken ct = default);

    /// <summary>Checks whether a language with the given BCP-47 code already exists.</summary>
    Task<bool> ExistsByCodeAsync(string bcp47Code, CancellationToken ct = default);
}

// ─── Translation ──────────────────────────────────────────────────────────────

/// <summary>
/// Write repository for <see cref="Translation"/>.
/// Extends <see cref="IRepository{T,TId}"/> with i18n-specific query methods.
/// </summary>
public interface ITranslationRepository : IRepository<Translation, Guid>
{
    /// <summary>
    /// Finds a single translation by its unique code and language.
    /// Returns <c>null</c> when not found — callers must then walk the fallback chain.
    /// </summary>
    Task<Translation?> FindAsync(
        TranslationCode code,
        LanguageCode    languageCode,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all translations for a specific module (e.g. <c>"CRM"</c>) and language.
    /// Used to build the full string table for a module in a single query.
    /// </summary>
    Task<IReadOnlyList<Translation>> GetByModuleAndLanguageAsync(
        string           module,
        LanguageCode     languageCode,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all language variants available for a given translation code.
    /// Used in admin tooling to identify which languages are missing a translation.
    /// </summary>
    Task<IReadOnlyList<Translation>> GetVariantsAsync(
        TranslationCode code,
        CancellationToken ct = default);

    /// <summary>
    /// Returns a paged list of translations, optionally filtered by module and/or language.
    /// </summary>
    Task<(IReadOnlyList<Translation> Items, long TotalCount)> GetPagedAsync(
        int               pageNumber,
        int               pageSize,
        string?           moduleFilter   = null,
        string?           languageFilter = null,
        CancellationToken ct             = default);

    /// <summary>
    /// Checks whether a translation with the given code and language already exists.
    /// Used to enforce the unique (code + language) business constraint.
    /// </summary>
    Task<bool> ExistsAsync(
        TranslationCode code,
        LanguageCode    languageCode,
        CancellationToken ct = default);

    /// <summary>Physically removes a translation entry from the database.</summary>
    void Remove(Translation translation);
}

// ─── LocaleConfiguration ──────────────────────────────────────────────────────

/// <summary>Write repository for <see cref="LocaleConfiguration"/>.</summary>
public interface ILocaleConfigurationRepository : IRepository<LocaleConfiguration, Guid>
{
    /// <summary>Returns the locale configuration for the given language, or <c>null</c>.</summary>
    Task<LocaleConfiguration?> GetByLanguageAsync(
        LanguageCode      languageCode,
        CancellationToken ct = default);

    /// <summary>Returns all locale configurations.</summary>
    Task<IReadOnlyList<LocaleConfiguration>> GetAllAsync(CancellationToken ct = default);
}

// ─── CurrencyFormat ───────────────────────────────────────────────────────────

/// <summary>Write repository for <see cref="CurrencyFormat"/>.</summary>
public interface ICurrencyFormatRepository : IRepository<CurrencyFormat, Guid>
{
    /// <summary>Finds a currency format for a language + ISO 4217 code pair, or <c>null</c>.</summary>
    Task<CurrencyFormat?> FindAsync(
        LanguageCode      languageCode,
        string            currencyCode,
        CancellationToken ct = default);

    /// <summary>Returns all currency formats configured for a given language.</summary>
    Task<IReadOnlyList<CurrencyFormat>> GetByLanguageAsync(
        LanguageCode      languageCode,
        CancellationToken ct = default);

    /// <summary>Removes a currency format entry from the database.</summary>
    void Remove(CurrencyFormat currencyFormat);
}
