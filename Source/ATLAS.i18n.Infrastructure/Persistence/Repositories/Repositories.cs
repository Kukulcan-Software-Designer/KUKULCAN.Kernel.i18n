using ATLAS.i18n.Application.Common.Interfaces;
using ATLAS.i18n.Domain.Entities;
using ATLAS.i18n.Domain.Repositories;
using ATLAS.i18n.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ATLAS.i18n.Infrastructure.Persistence.Repositories;

// ─── Unit of Work ─────────────────────────────────────────────────────────────

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly I18nDbContext _context;

    public UnitOfWork(I18nDbContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}

// ─── Language Repository ──────────────────────────────────────────────────────

public sealed class LanguageRepository : ILanguageRepository
{
    private readonly I18nDbContext _context;

    public LanguageRepository(I18nDbContext context) => _context = context;

    public async Task<Language?> GetByCodeAsync(LanguageCode code, CancellationToken ct = default)
        => await _context.Languages
            .Include(l => l.LocaleConfiguration)
            .Include(l => l.CurrencyFormats)
            .FirstOrDefaultAsync(l => l.Id == code, ct);

    public async Task<IReadOnlyList<Language>> GetAllActiveAsync(CancellationToken ct = default)
        => await _context.Languages
            .Where(l => l.IsActive)
            .OrderBy(l => l.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Language>> GetAllAsync(CancellationToken ct = default)
        => await _context.Languages
            .OrderBy(l => l.Name)
            .ToListAsync(ct);

    public async Task<Language?> GetDefaultAsync(CancellationToken ct = default)
        => await _context.Languages
            .FirstOrDefaultAsync(l => l.IsDefault, ct);

    public async Task<bool> ExistsAsync(LanguageCode code, CancellationToken ct = default)
        => await _context.Languages.AnyAsync(l => l.Id == code, ct);

    public void Add(Language language)
        => _context.Languages.Add(language);

    public void Update(Language language)
        => _context.Languages.Update(language);

    public void Remove(Language language)
        => _context.Languages.Remove(language);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}

// ─── Translation Repository ───────────────────────────────────────────────────

public sealed class TranslationRepository : ITranslationRepository
{
    private readonly I18nDbContext _context;

    public TranslationRepository(I18nDbContext context) => _context = context;

    public async Task<Translation?> FindAsync(
        TranslationCode code,
        LanguageCode languageCode,
        CancellationToken ct = default)
        => await _context.Translations
            .FirstOrDefaultAsync(
                t => t.Code == code && t.LanguageCode == languageCode, ct);

    public async Task<Translation?> FindWithFallbackAsync(
        TranslationCode code,
        LanguageCode requestedLanguage,
        CancellationToken ct = default)
    {
        var translation = await FindAsync(code, requestedLanguage, ct);
        if (translation is not null) return translation;

        if (requestedLanguage != LanguageCode.English)
            return await FindAsync(code, LanguageCode.English, ct);

        return null;
    }

    public async Task<IReadOnlyList<Translation>> GetByLanguageAsync(
        LanguageCode languageCode,
        CancellationToken ct = default)
        => await _context.Translations
            .Where(t => t.LanguageCode == languageCode)
            .OrderBy(t => t.Code)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Translation>> GetByModuleAndLanguageAsync(
        string module,
        LanguageCode languageCode,
        CancellationToken ct = default)
    {
        var prefix = module.ToUpperInvariant();
        return await _context.Translations
            .Where(t => t.LanguageCode == languageCode
                     && EF.Functions.Like(EF.Property<string>(t, "Code"), $"{prefix}%"))
            .OrderBy(t => t.Code)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Translation>> GetAllVariantsAsync(
        TranslationCode code,
        CancellationToken ct = default)
        => await _context.Translations
            .Where(t => t.Code == code)
            .OrderBy(t => t.LanguageCode)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Translation>> GetAllAsync(CancellationToken ct = default)
        => await _context.Translations
            .OrderBy(t => t.Code)
            .ThenBy(t => t.LanguageCode)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<Translation> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? moduleFilter   = null,
        string? languageFilter = null,
        CancellationToken ct   = default)
    {
        var query = _context.Translations.AsQueryable();

        if (moduleFilter is not null)
            query = query.Where(t =>
                EF.Functions.Like(EF.Property<string>(t, "Code"),
                    $"{moduleFilter.ToUpperInvariant()}%"));

        if (languageFilter is not null)
        {
            var lang = LanguageCode.From(languageFilter);
            query = query.Where(t => t.LanguageCode == lang);
        }

        var total = await query.CountAsync(ct);
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
        => await _context.Translations
            .AnyAsync(t => t.Code == code && t.LanguageCode == languageCode, ct);

    public void Add(Translation translation)      => _context.Translations.Add(translation);
    public void Update(Translation translation)   => _context.Translations.Update(translation);
    public void Remove(Translation translation)   => _context.Translations.Remove(translation);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}

// ─── LocaleConfiguration Repository ──────────────────────────────────────────

public sealed class LocaleConfigurationRepository : ILocaleConfigurationRepository
{
    private readonly I18nDbContext _context;

    public LocaleConfigurationRepository(I18nDbContext context) => _context = context;

    public async Task<LocaleConfiguration?> GetByLanguageAsync(
        LanguageCode languageCode,
        CancellationToken ct = default)
        => await _context.LocaleConfigurations
            .FirstOrDefaultAsync(lc => lc.LanguageCode == languageCode, ct);

    public async Task<IReadOnlyList<LocaleConfiguration>> GetAllAsync(CancellationToken ct = default)
        => await _context.LocaleConfigurations
            .OrderBy(lc => lc.LanguageCode)
            .ToListAsync(ct);

    public async Task<bool> ExistsAsync(LanguageCode languageCode, CancellationToken ct = default)
        => await _context.LocaleConfigurations
            .AnyAsync(lc => lc.LanguageCode == languageCode, ct);

    public void Add(LocaleConfiguration configuration)    => _context.LocaleConfigurations.Add(configuration);
    public void Update(LocaleConfiguration configuration) => _context.LocaleConfigurations.Update(configuration);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}

// ─── CurrencyFormat Repository ────────────────────────────────────────────────

public sealed class CurrencyFormatRepository : ICurrencyFormatRepository
{
    private readonly I18nDbContext _context;

    public CurrencyFormatRepository(I18nDbContext context) => _context = context;

    public async Task<CurrencyFormat?> FindAsync(
        LanguageCode languageCode,
        string currencyCode,
        CancellationToken ct = default)
        => await _context.CurrencyFormats
            .FirstOrDefaultAsync(
                cf => cf.LanguageCode == languageCode
                   && cf.CurrencyCode == currencyCode.ToUpperInvariant(), ct);

    public async Task<IReadOnlyList<CurrencyFormat>> GetByLanguageAsync(
        LanguageCode languageCode,
        CancellationToken ct = default)
        => await _context.CurrencyFormats
            .Where(cf => cf.LanguageCode == languageCode)
            .OrderBy(cf => cf.CurrencyCode)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CurrencyFormat>> GetByCurrencyAsync(
        string currencyCode,
        CancellationToken ct = default)
        => await _context.CurrencyFormats
            .Where(cf => cf.CurrencyCode == currencyCode.ToUpperInvariant())
            .OrderBy(cf => cf.LanguageCode)
            .ToListAsync(ct);

    public async Task<bool> ExistsAsync(
        LanguageCode languageCode,
        string currencyCode,
        CancellationToken ct = default)
        => await _context.CurrencyFormats
            .AnyAsync(cf => cf.LanguageCode == languageCode
                         && cf.CurrencyCode == currencyCode.ToUpperInvariant(), ct);

    public void Add(CurrencyFormat currencyFormat)    => _context.CurrencyFormats.Add(currencyFormat);
    public void Update(CurrencyFormat currencyFormat) => _context.CurrencyFormats.Update(currencyFormat);
    public void Remove(CurrencyFormat currencyFormat) => _context.CurrencyFormats.Remove(currencyFormat);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
