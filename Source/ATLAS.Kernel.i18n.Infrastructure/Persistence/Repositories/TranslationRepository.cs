namespace ATLAS.Kernel.i18n.Infrastructure.Persistence.Repositories;

public sealed class TranslationRepository(I18nDbContext context) : ITranslationRepository
{
    public async Task<Translation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Translations.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Translation?> FindAsync(
        TranslationCode code,
        LanguageCode languageCode,
        CancellationToken ct = default)
        => await context.Translations
            .FirstOrDefaultAsync(t =>
                t.Code == code &&
                t.LanguageCode == languageCode, ct);

    public async Task<IReadOnlyList<Translation>> ListAllAsync(CancellationToken ct = default)
        => await context.Translations
            .OrderBy(t => t.Code)
            .ThenBy(t => t.LanguageCode)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Translation>> GetByModuleAndLanguageAsync(
        string module,
        LanguageCode languageCode,
        CancellationToken ct = default)
    {
        var prefix = module.ToUpperInvariant();
        return await context.Translations
            .Where(t =>
                t.LanguageCode == languageCode &&
                EF.Functions.Like(
                    EF.Property<string>(t, "Code"),
                    $"{prefix}%"))
            .OrderBy(t => t.Code)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Translation>> GetVariantsAsync(
        TranslationCode code,
        CancellationToken ct = default)
        => await context.Translations
            .Where(t => t.Code == code)
            .OrderBy(t => t.LanguageCode)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<Translation> Items, long TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? moduleFilter = null,
        string? languageFilter = null,
        CancellationToken ct = default)
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

    public async Task<bool> ExistsAsync(
        TranslationCode code,
        LanguageCode languageCode,
        CancellationToken ct = default)
        => await context.Translations.AnyAsync(
            t => t.Code == code && t.LanguageCode == languageCode, ct);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await context.Translations.AnyAsync(t => t.Id == id, ct);

    public async Task AddAsync(Translation translation, CancellationToken ct = default)
        => await context.Translations.AddAsync(translation, ct);

    public void Update(Translation translation)
        => context.Translations.Update(translation);

    public void Remove(Translation translation)
        => context.Translations.Remove(translation);
}
