using ATLAS.i18n.Domain.Entities;
using ATLAS.i18n.Domain.ValueObjects;

namespace ATLAS.i18n.Domain.Repositories;

/// <summary>
/// Repository interface for <see cref="Language"/> aggregate.
/// </summary>
public interface ILanguageRepository
{
    /// <summary>Returns the language with all owned data, or null if not found.</summary>
    Task<Language?> GetByCodeAsync(LanguageCode code, CancellationToken ct = default);

    /// <summary>Returns all active languages.</summary>
    Task<IReadOnlyList<Language>> GetAllActiveAsync(CancellationToken ct = default);

    /// <summary>Returns all languages (including inactive).</summary>
    Task<IReadOnlyList<Language>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns the configured default language (English by convention).</summary>
    Task<Language?> GetDefaultAsync(CancellationToken ct = default);

    /// <summary>Returns true if the given code already exists in the database.</summary>
    Task<bool> ExistsAsync(LanguageCode code, CancellationToken ct = default);

    void Add(Language language);
    void Update(Language language);
    void Remove(Language language);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>
/// Repository interface for <see cref="Translation"/> aggregate.
/// </summary>
public interface ITranslationRepository
{
    /// <summary>
    /// Finds a translation by its unique code and language.
    /// Returns null if not found (callers should fall back to English).
    /// </summary>
    Task<Translation?> FindAsync(
        TranslationCode code,
        LanguageCode languageCode,
        CancellationToken ct = default);

    /// <summary>
    /// Finds a translation by code and language. If not found, falls back to the default
    /// language (EN). Returns null only if the English fallback is also missing.
    /// </summary>
    Task<Translation?> FindWithFallbackAsync(
        TranslationCode code,
        LanguageCode requestedLanguage,
        CancellationToken ct = default);

    /// <summary>Returns all translations for a given language.</summary>
    Task<IReadOnlyList<Translation>> GetByLanguageAsync(
        LanguageCode languageCode,
        CancellationToken ct = default);

    /// <summary>Returns all translations for a specific module (e.g. "CRM") and language.</summary>
    Task<IReadOnlyList<Translation>> GetByModuleAndLanguageAsync(
        string module,
        LanguageCode languageCode,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all language variants for a given code.
    /// Useful to check which languages a particular string has been translated into.
    /// </summary>
    Task<IReadOnlyList<Translation>> GetAllVariantsAsync(
        TranslationCode code,
        CancellationToken ct = default);

    /// <summary>Returns all translations in bulk (for export / cache warming).</summary>
    Task<IReadOnlyList<Translation>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns paged translations, optionally filtered by module and/or language.
    /// </summary>
    Task<(IReadOnlyList<Translation> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? moduleFilter = null,
        string? languageFilter = null,
        CancellationToken ct = default);

    Task<bool> ExistsAsync(
        TranslationCode code,
        LanguageCode languageCode,
        CancellationToken ct = default);

    void Add(Translation translation);
    void Update(Translation translation);
    void Remove(Translation translation);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>
/// Repository interface for <see cref="LocaleConfiguration"/> entities.
/// </summary>
public interface ILocaleConfigurationRepository
{
    Task<LocaleConfiguration?> GetByLanguageAsync(
        LanguageCode languageCode,
        CancellationToken ct = default);

    Task<IReadOnlyList<LocaleConfiguration>> GetAllAsync(CancellationToken ct = default);

    Task<bool> ExistsAsync(LanguageCode languageCode, CancellationToken ct = default);

    void Add(LocaleConfiguration configuration);
    void Update(LocaleConfiguration configuration);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>
/// Repository interface for <see cref="CurrencyFormat"/> entities.
/// </summary>
public interface ICurrencyFormatRepository
{
    Task<CurrencyFormat?> FindAsync(
        LanguageCode languageCode,
        string currencyCode,
        CancellationToken ct = default);

    Task<IReadOnlyList<CurrencyFormat>> GetByLanguageAsync(
        LanguageCode languageCode,
        CancellationToken ct = default);

    Task<IReadOnlyList<CurrencyFormat>> GetByCurrencyAsync(
        string currencyCode,
        CancellationToken ct = default);

    Task<bool> ExistsAsync(
        LanguageCode languageCode,
        string currencyCode,
        CancellationToken ct = default);

    void Add(CurrencyFormat currencyFormat);
    void Update(CurrencyFormat currencyFormat);
    void Remove(CurrencyFormat currencyFormat);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
