using ATLAS.i18n.Domain.Entities;
using ATLAS.i18n.Domain.ValueObjects;
using ATLAS.i18n.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace ATLAS.i18n.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the ATLAS.i18n service.
/// Schema: i18n (isolated within the shared ATLAS database, or a dedicated DB).
/// </summary>
public sealed class I18nDbContext : DbContext
{
    public I18nDbContext(DbContextOptions<I18nDbContext> options)
        : base(options) { }

    public DbSet<Language>             Languages             => Set<Language>();
    public DbSet<Translation>          Translations          => Set<Translation>();
    public DbSet<LocaleConfiguration>  LocaleConfigurations  => Set<LocaleConfiguration>();
    public DbSet<CurrencyFormat>       CurrencyFormats       => Set<CurrencyFormat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("i18n");

        // Apply all IEntityTypeConfiguration classes from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(I18nDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Map all LanguageCode value objects as VARCHAR(2)
        configurationBuilder
            .Properties<LanguageCode>()
            .HaveMaxLength(2);

        // Map all TranslationCode value objects as VARCHAR(9) (5 module + 4 digits)
        configurationBuilder
            .Properties<TranslationCode>()
            .HaveMaxLength(9);
    }
}
