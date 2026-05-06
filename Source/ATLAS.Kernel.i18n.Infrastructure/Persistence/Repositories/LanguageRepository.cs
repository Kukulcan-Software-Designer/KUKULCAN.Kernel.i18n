namespace ATLAS.Kernel.i18n.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ILanguageRepository"/>.
/// Base CRUD operations are provided by the repository directly via DbContext —
/// the SharedKernel defines the interface, not a generic base repository class.
/// </summary>
public sealed class LanguageRepository(I18nDbContext context) : ILanguageRepository
{
    public async Task<Language?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Languages
            .Include(l => l.LocaleConfiguration)
            .Include(l => l.CurrencyFormats)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<Language?> GetByCodeAsync(string bcp47Code, CancellationToken ct = default)
        => await context.Languages
            .Include(l => l.LocaleConfiguration)
            .Include(l => l.CurrencyFormats)
            .FirstOrDefaultAsync(l => l.Code == bcp47Code, ct);

    public async Task<IReadOnlyList<Language>> ListAllAsync(CancellationToken ct = default)
        => await context.Languages
            .OrderBy(l => l.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Language>> GetAllActiveAsync(CancellationToken ct = default)
        => await context.Languages
            .Where(l => l.IsActive)
            .OrderBy(l => l.Name)
            .ToListAsync(ct);

    public async Task<Language?> GetDefaultAsync(CancellationToken ct = default)
        => await context.Languages
            .FirstOrDefaultAsync(l => l.IsDefault, ct);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await context.Languages.AnyAsync(l => l.Id == id, ct);

    public async Task<bool> ExistsByCodeAsync(string bcp47Code, CancellationToken ct = default)
        => await context.Languages.AnyAsync(l => l.Code == bcp47Code, ct);

    public async Task AddAsync(Language language, CancellationToken ct = default)
        => await context.Languages.AddAsync(language, ct);

    public void Update(Language language)
        => context.Languages.Update(language);
}
