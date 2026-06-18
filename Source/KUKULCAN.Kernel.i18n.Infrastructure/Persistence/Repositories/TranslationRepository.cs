namespace KUKULCAN.Kernel.i18n.Infrastructure.Persistence.Repositories;

/// <summary>
/// Represents the TranslationRepository type.
/// </summary>
/// <param name="context">The database context.</param>
public sealed class TranslationRepository(I18NDbContext context) : ITranslationRepository
{
    /// <summary>
    /// Executes GetByIdAsync.
    /// </summary>
    /// <param name="id">The id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The operation result.</returns>
    public async Task<Translation?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Translations.FirstOrDefaultAsync(t => t.Id == id, ct);

    /// <summary>
    /// Executes FindAsync.
    /// </summary>
    /// <param name="code">The code parameter.</param>
    /// <param name="languageCode">The languageCode parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The operation result.</returns>
    public async Task<Translation?> FindAsync(TranslationCode code, LanguageCode languageCode, CancellationToken ct = default) =>
        await context.Translations.FirstOrDefaultAsync(t => t.Code == code && t.LanguageCode == languageCode, ct);

    /// <summary>
    /// Executes ListAllAsync.
    /// </summary>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The operation result.</returns>
    public async Task<IReadOnlyList<Translation>> ListAllAsync(CancellationToken ct = default) =>
        await context.Translations.OrderBy(t => t.Code).ThenBy(t => t.LanguageCode).ToListAsync(ct);

    /// <summary>
    /// Executes GetByModuleAndLanguageAsync.
    /// </summary>
    /// <param name="module">The module parameter.</param>
    /// <param name="languageCode">The languageCode parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The operation result.</returns>
    public async Task<IReadOnlyList<Translation>> GetByModuleAndLanguageAsync(string module, LanguageCode languageCode, CancellationToken ct = default)
    {
        var prefix = module.ToUpperInvariant();

        return await context.Translations
            .Where(t =>
                t.LanguageCode == languageCode && EF.Functions.Like(EF.Property<string>(t, "Code"), $"{prefix}%"))
            .OrderBy(t => t.Code)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Executes GetVariantsAsync.
    /// </summary>
    /// <param name="code">The code parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The operation result.</returns>
    public async Task<IReadOnlyList<Translation>> GetVariantsAsync(TranslationCode code, CancellationToken ct = default) =>
        await context.Translations
            .Where(t => t.Code == code)
            .OrderBy(t => t.LanguageCode)
            .ToListAsync(ct);

    /// <summary>
    /// Executes GetPagedAsync.
    /// </summary>
    /// <param name="pageNumber">The pageNumber parameter.</param>
    /// <param name="pageSize">The pageSize parameter.</param>
    /// <param name="moduleFilter">The moduleFilter parameter.</param>
    /// <param name="languageFilter">The languageFilter parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The operation result.</returns>
    public async Task<(IReadOnlyList<Translation> Items, long TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? moduleFilter = null,
        string? languageFilter = null, CancellationToken ct = default)
    {
        var query = context.Translations.AsQueryable();

        if (moduleFilter is not null)
            query = query.Where(t => EF.Functions.Like(
                EF.Property<string>(t, "Code"),
                $"{moduleFilter.ToUpperInvariant()}%"));

        if (languageFilter is not null)
        {
            var langResult = LanguageCode.Create(languageFilter);
            if (langResult.IsSuccess)
                query = query.Where(t => t.LanguageCode == langResult.Value);
        }

        var total = await query.LongCountAsync(ct);
        var items = await query
            .OrderBy(t => t.Code)
            .ThenBy(t => t.LanguageCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    /// <summary>
    /// Executes ExistsAsync.
    /// </summary>
    /// <param name="code">The code parameter.</param>
    /// <param name="languageCode">The languageCode parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The operation result.</returns>
    public async Task<bool> ExistsAsync(TranslationCode code, LanguageCode languageCode, CancellationToken ct = default) =>
        await context.Translations.AnyAsync(t => t.Code == code && t.LanguageCode == languageCode, ct);

    /// <summary>
    /// Executes ExistsAsync.
    /// </summary>
    /// <param name="id">The id parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The operation result.</returns>
    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
        await context.Translations.AnyAsync(t => t.Id == id, ct);

    /// <summary>
    /// Executes AddAsync.
    /// </summary>
    /// <param name="translation">The translation parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The operation result.</returns>
    public async Task AddAsync(Translation translation, CancellationToken ct = default) =>
        await context.Translations.AddAsync(translation, ct);

    /// <summary>
    /// Executes Update.
    /// </summary>
    /// <param name="translation">The translation parameter.</param>
    public void Update(Translation translation) =>
        context.Translations.Update(translation);

    /// <summary>
    /// Executes Remove.
    /// </summary>
    /// <param name="translation">The translation parameter.</param>
    public void Remove(Translation translation) =>
        context.Translations.Remove(translation);
}
