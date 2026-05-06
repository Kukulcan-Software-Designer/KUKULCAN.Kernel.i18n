namespace ATLAS.Kernel.i18n.Infrastructure.Persistence.Repositories;

public sealed class CurrencyFormatRepository(I18nDbContext context) : ICurrencyFormatRepository
{
    public async Task<CurrencyFormat?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.CurrencyFormats.FirstOrDefaultAsync(cf => cf.Id == id, ct);

    public async Task<CurrencyFormat?> FindAsync(
        LanguageCode languageCode,
        string currencyCode,
        CancellationToken ct = default)
        => await context.CurrencyFormats
            .FirstOrDefaultAsync(cf =>
                cf.LanguageCode == languageCode &&
                cf.CurrencyCode == currencyCode.ToUpperInvariant(), ct);

    public async Task<IReadOnlyList<CurrencyFormat>> ListAllAsync(CancellationToken ct = default)
        => await context.CurrencyFormats
            .OrderBy(cf => cf.LanguageCode)
            .ThenBy(cf => cf.CurrencyCode)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CurrencyFormat>> GetByLanguageAsync(
        LanguageCode languageCode,
        CancellationToken ct = default)
        => await context.CurrencyFormats
            .Where(cf => cf.LanguageCode == languageCode)
            .OrderBy(cf => cf.CurrencyCode)
            .ToListAsync(ct);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await context.CurrencyFormats.AnyAsync(cf => cf.Id == id, ct);

    public async Task AddAsync(CurrencyFormat format, CancellationToken ct = default)
        => await context.CurrencyFormats.AddAsync(format, ct);

    public void Update(CurrencyFormat format)
        => context.CurrencyFormats.Update(format);

    public void Remove(CurrencyFormat format)
        => context.CurrencyFormats.Remove(format);
}
