using ATLAS.i18n.Infrastructure.Persistence;

namespace ATLAS.i18n.Infrastructure.Persistence.Repositories;

// ─── Language Repository ──────────────────────────────────────────────────────

/// <summary>
/// EF Core implementation of <see cref="ILanguageRepository"/>.
/// Base CRUD operations are provided by the repository directly via DbContext —
/// the SharedKernel defines the interface, not a generic base repository class.
/// </summary>
public sealed class LanguageRepository : ILanguageRepository
{
    private readonly I18nDbContext _context;

    public LanguageRepository(I18nDbContext context) => _context = context;

    public async Task<Language?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Languages
            .Include(l => l.LocaleConfiguration)
            .Include(l => l.CurrencyFormats)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<Language?> GetByCodeAsync(string bcp47Code, CancellationToken ct = default)
        => await _context.Languages
            .Include(l => l.LocaleConfiguration)
            .Include(l => l.CurrencyFormats)
            .FirstOrDefaultAsync(l => l.Code == bcp47Code, ct);

    public async Task<IReadOnlyList<Language>> ListAllAsync(CancellationToken ct = default)
        => await _context.Languages
            .OrderBy(l => l.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Language>> GetAllActiveAsync(CancellationToken ct = default)
        => await _context.Languages
            .Where(l => l.IsActive)
            .OrderBy(l => l.Name)
            .ToListAsync(ct);

    public async Task<Language?> GetDefaultAsync(CancellationToken ct = default)
        => await _context.Languages
            .FirstOrDefaultAsync(l => l.IsDefault, ct);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await _context.Languages.AnyAsync(l => l.Id == id, ct);

    public async Task<bool> ExistsByCodeAsync(string bcp47Code, CancellationToken ct = default)
        => await _context.Languages.AnyAsync(l => l.Code == bcp47Code, ct);

    public async Task AddAsync(Language language, CancellationToken ct = default)
        => await _context.Languages.AddAsync(language, ct);

    public void Update(Language language)
        => _context.Languages.Update(language);
}

// ─── Translation Repository ───────────────────────────────────────────────────

public sealed class TranslationRepository : ITranslationRepository
{
    private readonly I18nDbContext _context;

    public TranslationRepository(I18nDbContext context) => _context = context;

    public async Task<Translation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Translations.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Translation?> FindAsync(
        TranslationCode code,
        LanguageCode    languageCode,
        CancellationToken ct = default)
        => await _context.Translations
            .FirstOrDefaultAsync(t =>
                t.Code == code &&
                t.LanguageCode == languageCode, ct);

    public async Task<IReadOnlyList<Translation>> ListAllAsync(CancellationToken ct = default)
        => await _context.Translations
            .OrderBy(t => t.Code)
            .ThenBy(t => t.LanguageCode)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Translation>> GetByModuleAndLanguageAsync(
        string           module,
        LanguageCode     languageCode,
        CancellationToken ct = default)
    {
        var prefix = module.ToUpperInvariant();
        return await _context.Translations
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
        => await _context.Translations
            .Where(t => t.Code == code)
            .OrderBy(t => t.LanguageCode)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<Translation> Items, long TotalCount)> GetPagedAsync(
        int               pageNumber,
        int               pageSize,
        string?           moduleFilter   = null,
        string?           languageFilter = null,
        CancellationToken ct             = default)
    {
        var query = _context.Translations.AsQueryable();

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
        LanguageCode    languageCode,
        CancellationToken ct = default)
        => await _context.Translations.AnyAsync(
            t => t.Code == code && t.LanguageCode == languageCode, ct);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await _context.Translations.AnyAsync(t => t.Id == id, ct);

    public async Task AddAsync(Translation translation, CancellationToken ct = default)
        => await _context.Translations.AddAsync(translation, ct);

    public void Update(Translation translation)
        => _context.Translations.Update(translation);

    public void Remove(Translation translation)
        => _context.Translations.Remove(translation);
}

// ─── LocaleConfiguration Repository ──────────────────────────────────────────

public sealed class LocaleConfigurationRepository : ILocaleConfigurationRepository
{
    private readonly I18nDbContext _context;

    public LocaleConfigurationRepository(I18nDbContext context) => _context = context;

    public async Task<LocaleConfiguration?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.LocaleConfigurations.FirstOrDefaultAsync(lc => lc.Id == id, ct);

    public async Task<LocaleConfiguration?> GetByLanguageAsync(
        LanguageCode      languageCode,
        CancellationToken ct = default)
        => await _context.LocaleConfigurations
            .FirstOrDefaultAsync(lc => lc.LanguageCode == languageCode, ct);

    public async Task<IReadOnlyList<LocaleConfiguration>> ListAllAsync(CancellationToken ct = default)
        => await _context.LocaleConfigurations
            .OrderBy(lc => lc.LanguageCode)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LocaleConfiguration>> GetAllAsync(CancellationToken ct = default)
        => await ListAllAsync(ct);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await _context.LocaleConfigurations.AnyAsync(lc => lc.Id == id, ct);

    public async Task AddAsync(LocaleConfiguration config, CancellationToken ct = default)
        => await _context.LocaleConfigurations.AddAsync(config, ct);

    public void Update(LocaleConfiguration config)
        => _context.LocaleConfigurations.Update(config);
}

// ─── CurrencyFormat Repository ────────────────────────────────────────────────

public sealed class CurrencyFormatRepository : ICurrencyFormatRepository
{
    private readonly I18nDbContext _context;

    public CurrencyFormatRepository(I18nDbContext context) => _context = context;

    public async Task<CurrencyFormat?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.CurrencyFormats.FirstOrDefaultAsync(cf => cf.Id == id, ct);

    public async Task<CurrencyFormat?> FindAsync(
        LanguageCode      languageCode,
        string            currencyCode,
        CancellationToken ct = default)
        => await _context.CurrencyFormats
            .FirstOrDefaultAsync(cf =>
                cf.LanguageCode == languageCode &&
                cf.CurrencyCode == currencyCode.ToUpperInvariant(), ct);

    public async Task<IReadOnlyList<CurrencyFormat>> ListAllAsync(CancellationToken ct = default)
        => await _context.CurrencyFormats
            .OrderBy(cf => cf.LanguageCode)
            .ThenBy(cf => cf.CurrencyCode)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CurrencyFormat>> GetByLanguageAsync(
        LanguageCode      languageCode,
        CancellationToken ct = default)
        => await _context.CurrencyFormats
            .Where(cf => cf.LanguageCode == languageCode)
            .OrderBy(cf => cf.CurrencyCode)
            .ToListAsync(ct);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await _context.CurrencyFormats.AnyAsync(cf => cf.Id == id, ct);

    public async Task AddAsync(CurrencyFormat format, CancellationToken ct = default)
        => await _context.CurrencyFormats.AddAsync(format, ct);

    public void Update(CurrencyFormat format)
        => _context.CurrencyFormats.Update(format);

    public void Remove(CurrencyFormat format)
        => _context.CurrencyFormats.Remove(format);
}
