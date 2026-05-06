namespace ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;

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
    Task<Translation?> FindAsync(TranslationCode code, LanguageCode languageCode, CancellationToken ct = default);

    /// <summary>
    /// Returns all translations for a specific module (e.g. <c>"CRM"</c>) and language.
    /// Used to build the full string table for a module in a single query.
    /// </summary>
    Task<IReadOnlyList<Translation>> GetByModuleAndLanguageAsync(string module, LanguageCode languageCode, CancellationToken ct = default);

    /// <summary>
    /// Returns all language variants available for a given translation code.
    /// Used in admin tooling to identify which languages are missing a translation.
    /// </summary>
    Task<IReadOnlyList<Translation>> GetVariantsAsync(TranslationCode code, CancellationToken ct = default);

    /// <summary>
    /// Returns a paged list of translations, optionally filtered by module and/or language.
    /// </summary>
    Task<(IReadOnlyList<Translation> Items, long TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? moduleFilter = null,
        string? languageFilter = null, CancellationToken ct = default);

    /// <summary>
    /// Checks whether a translation with the given code and language already exists.
    /// Used to enforce the unique (code + language) business constraint.
    /// </summary>
    Task<bool> ExistsAsync(TranslationCode code, LanguageCode languageCode, CancellationToken ct = default);

    /// <summary>Physically removes a translation entry from the database.</summary>
    void Remove(Translation translation);
}
