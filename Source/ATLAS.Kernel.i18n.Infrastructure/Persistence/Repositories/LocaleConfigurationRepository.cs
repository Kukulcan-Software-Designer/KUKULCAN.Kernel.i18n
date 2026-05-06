namespace ATLAS.Kernel.i18n.Infrastructure.Persistence.Repositories;

public sealed class LocaleConfigurationRepository(I18nDbContext context) : ILocaleConfigurationRepository
{
    public async Task<LocaleConfiguration?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.LocaleConfigurations.FirstOrDefaultAsync(lc => lc.Id == id, ct);

    public async Task<LocaleConfiguration?> GetByLanguageAsync(
        LanguageCode languageCode,
        CancellationToken ct = default)
        => await context.LocaleConfigurations
            .FirstOrDefaultAsync(lc => lc.LanguageCode == languageCode, ct);

    public async Task<IReadOnlyList<LocaleConfiguration>> ListAllAsync(CancellationToken ct = default)
        => await context.LocaleConfigurations
            .OrderBy(lc => lc.LanguageCode)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LocaleConfiguration>> GetAllAsync(CancellationToken ct = default)
        => await ListAllAsync(ct);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await context.LocaleConfigurations.AnyAsync(lc => lc.Id == id, ct);

    public async Task AddAsync(LocaleConfiguration config, CancellationToken ct = default)
        => await context.LocaleConfigurations.AddAsync(config, ct);

    public void Update(LocaleConfiguration config)
        => context.LocaleConfigurations.Update(config);
}
