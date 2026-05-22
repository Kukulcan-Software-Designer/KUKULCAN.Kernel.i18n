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
    /// <param name="code">The translation code.</param>
    /// <param name="languageCode">The language code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The translation if found; otherwise, <c>null</c>.  </returns>
    Task<Translation?> FindAsync(TranslationCode code, LanguageCode languageCode, CancellationToken ct = default);

    /// <summary>
    /// Returns all translations for a specific module (e.g. <c>"CRM"</c>) and language.
    /// Used to build the full string table for a module in a single query.
    /// </summary>
    /// <param name="module">The module name.</param>
    /// <param name="languageCode">The language code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of translations for the specified module and language.</returns>
    Task<IReadOnlyList<Translation>> GetByModuleAndLanguageAsync(string module, LanguageCode languageCode, CancellationToken ct = default);

    /// <summary>
    /// Returns all language variants available for a given translation code.
    /// Used in admin tooling to identify which languages are missing a translation.
    /// </summary>
    /// <param name="code">The translation code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of translations for the given code.</returns>
    Task<IReadOnlyList<Translation>> GetVariantsAsync(TranslationCode code, CancellationToken ct = default);

    /// <summary>
    /// Returns a paged list of translations, optionally filtered by module and/or language.
    /// </summary>
    /// <param name="pageNumber">The page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="moduleFilter">Optional module filter.</param>
    /// <param name="languageFilter">Optional language filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing the list of translations and the total count.</returns>
    Task<(IReadOnlyList<Translation> Items, long TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? moduleFilter = null, string? languageFilter = null, CancellationToken ct = default);

    /// <summary>
    /// Checks whether a translation with the given code and language already exists.
    /// Used to enforce the unique (code + language) business constraint.
    /// </summary>
    /// <param name="code">The translation code.</param>
    /// <param name="languageCode">The language code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the translation exists; otherwise, <c>false</c>.</returns>
    Task<bool> ExistsAsync(TranslationCode code, LanguageCode languageCode, CancellationToken ct = default);

    /// <summary>
    /// Physically removes a translation entry from the database.
    /// </summary>
    /// <param name="translation">The translation to remove.</param>
    void Remove(Translation translation);
}
