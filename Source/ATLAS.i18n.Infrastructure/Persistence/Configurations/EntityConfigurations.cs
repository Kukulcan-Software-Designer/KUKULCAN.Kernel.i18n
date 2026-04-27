using ATLAS.i18n.Infrastructure.Persistence;

namespace ATLAS.i18n.Infrastructure.Persistence.Configurations;

// ─── Language ─────────────────────────────────────────────────────────────────

/// <summary>
/// EF Core configuration for <see cref="Language"/>.
/// Maps the global language catalogue to the <c>i18n.Languages</c> table.
/// </summary>
public sealed class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.ToTable("Languages");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        // BCP-47 code is the unique business key (e.g. "es-ES")
        builder.Property(l => l.Code)
            .HasMaxLength(10)
            .IsRequired();

        builder.HasIndex(l => l.Code)
            .IsUnique()
            .HasDatabaseName("UX_Languages_Code");

        builder.Property(l => l.Name).HasMaxLength(100).IsRequired();
        builder.Property(l => l.NativeName).HasMaxLength(100).IsRequired();
        builder.Property(l => l.IsDefault).IsRequired();

        // IsActive comes from MasterEntity<Guid> via IActivatable
        builder.Property(l => l.IsActive).IsRequired();

        // Audit fields (IAuditable) — set by AuditSaveChangesInterceptor
        builder.Property(l => l.CreatedAt).IsRequired();
        builder.Property(l => l.CreatedBy).HasMaxLength(256).IsRequired();
        builder.Property(l => l.UpdatedAt);
        builder.Property(l => l.UpdatedBy).HasMaxLength(256);

        // Partial index: quickly find the single default language
        builder.HasIndex(l => l.IsDefault)
            .HasFilter("\"IsDefault\" = true")
            .HasDatabaseName("IX_Languages_Default");

        // Navigation — owned LocaleConfiguration (one-to-one)
        builder.HasOne(l => l.LocaleConfiguration)
            .WithOne()
            .HasForeignKey<LocaleConfiguration>("LanguageId")
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation — owned CurrencyFormats (one-to-many)
        builder.HasMany(l => l.CurrencyFormats)
            .WithOne()
            .HasForeignKey("LanguageId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// ─── Translation ──────────────────────────────────────────────────────────────

/// <summary>
/// EF Core configuration for <see cref="Translation"/>.
/// Maps translation entries to the <c>i18n.Translations</c> table.
/// </summary>
public sealed class TranslationConfiguration : IEntityTypeConfiguration<Translation>
{
    public void Configure(EntityTypeBuilder<Translation> builder)
    {
        builder.ToTable("Translations");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        // TranslationCode value object → stored as VARCHAR(9) (e.g. "ATLAS0001")
        builder.Property(t => t.Code)
            .HasColumnName("Code")
            .HasMaxLength(9)
            .HasConversion(
                v => v.Value,
                v => TranslationCode.From(v).Value)  // Result.Value safe — DB values are pre-validated
            .IsRequired();

        // LanguageCode value object (SharedKernel) → stored as VARCHAR(10) (e.g. "es-ES")
        builder.Property(t => t.LanguageCode)
            .HasColumnName("LanguageCode")
            .HasMaxLength(10)
            .HasConversion(
                v => v.Value,
                v => LanguageCode.Create(v).Value)
            .IsRequired();

        builder.Property(t => t.Text).HasMaxLength(4000).IsRequired();
        builder.Property(t => t.Context).HasMaxLength(500);
        builder.Property(t => t.MaxLength);
        builder.Property(t => t.IsReviewed).IsRequired();

        // Audit fields
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.CreatedBy).HasMaxLength(256).IsRequired();
        builder.Property(t => t.UpdatedAt);
        builder.Property(t => t.UpdatedBy).HasMaxLength(256);

        // Composite unique index: (Code + LanguageCode) is the natural business key
        builder.HasIndex(t => new { t.Code, t.LanguageCode })
            .IsUnique()
            .HasDatabaseName("UX_Translations_Code_Language");

        // Index for module queries — EF LIKE 'CRM%'
        builder.HasIndex(t => t.LanguageCode)
            .HasDatabaseName("IX_Translations_LanguageCode");

        // Soft FK to Language (restrict delete — language can't be deleted if translations exist)
        builder.HasOne<Language>()
            .WithMany()
            .HasForeignKey(t => t.LanguageCode)
            .HasPrincipalKey(l => l.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// ─── LocaleConfiguration ─────────────────────────────────────────────────────

/// <summary>
/// EF Core configuration for <see cref="LocaleConfiguration"/>.
/// Maps locale formatting rules to the <c>i18n.LocaleConfigurations</c> table.
/// </summary>
public sealed class LocaleConfigurationConfiguration
    : IEntityTypeConfiguration<LocaleConfiguration>
{
    public void Configure(EntityTypeBuilder<LocaleConfiguration> builder)
    {
        builder.ToTable("LocaleConfigurations");

        builder.HasKey(lc => lc.Id);
        builder.Property(lc => lc.Id).ValueGeneratedNever();

        // LanguageCode stored as VARCHAR — shadow FK to Language.Id
        builder.Property(lc => lc.LanguageCode)
            .HasColumnName("LanguageCode")
            .HasMaxLength(10)
            .HasConversion(
                v => v.Value,
                v => LanguageCode.Create(v).Value)
            .IsRequired();

        builder.HasIndex(lc => lc.LanguageCode)
            .IsUnique()
            .HasDatabaseName("UX_LocaleConfigurations_LanguageCode");

        builder.Property(lc => lc.DateFormat).HasMaxLength(50).IsRequired();
        builder.Property(lc => lc.ShortDateFormat).HasMaxLength(50).IsRequired();
        builder.Property(lc => lc.TimeFormat).HasMaxLength(50).IsRequired();
        builder.Property(lc => lc.DateTimeFormat).HasMaxLength(100).IsRequired();
        builder.Property(lc => lc.FirstDayOfWeek).IsRequired();

        // Store char separator as single-char string
        builder.Property(lc => lc.DecimalSeparator)
            .HasMaxLength(1)
            .HasConversion(c => c.ToString(), s => s[0])
            .IsRequired();

        builder.Property(lc => lc.ThousandsSeparator)
            .HasMaxLength(1)
            .HasConversion(c => c.ToString(), s => s[0])
            .IsRequired();

        builder.Property(lc => lc.DecimalPlaces).IsRequired();
        builder.Property(lc => lc.CurrencyDecimalPlaces).IsRequired();

        // Audit fields
        builder.Property(lc => lc.CreatedAt).IsRequired();
        builder.Property(lc => lc.CreatedBy).HasMaxLength(256).IsRequired();
        builder.Property(lc => lc.UpdatedAt);
        builder.Property(lc => lc.UpdatedBy).HasMaxLength(256);
    }
}

// ─── CurrencyFormat ───────────────────────────────────────────────────────────

/// <summary>
/// EF Core configuration for <see cref="CurrencyFormat"/>.
/// Maps currency formatting rules to the <c>i18n.CurrencyFormats</c> table.
/// </summary>
public sealed class CurrencyFormatConfiguration : IEntityTypeConfiguration<CurrencyFormat>
{
    public void Configure(EntityTypeBuilder<CurrencyFormat> builder)
    {
        builder.ToTable("CurrencyFormats");

        builder.HasKey(cf => cf.Id);
        builder.Property(cf => cf.Id).ValueGeneratedNever();

        builder.Property(cf => cf.LanguageCode)
            .HasColumnName("LanguageCode")
            .HasMaxLength(10)
            .HasConversion(
                v => v.Value,
                v => LanguageCode.Create(v).Value)
            .IsRequired();

        builder.Property(cf => cf.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(cf => cf.CurrencyName).HasMaxLength(100).IsRequired();
        builder.Property(cf => cf.Symbol).HasMaxLength(5).IsRequired();
        builder.Property(cf => cf.SymbolPosition).IsRequired();
        builder.Property(cf => cf.SpaceBetweenSymbolAndAmount).IsRequired();

        builder.Property(cf => cf.DecimalSeparator)
            .HasMaxLength(1)
            .HasConversion(c => c.ToString(), s => s[0])
            .IsRequired();

        builder.Property(cf => cf.ThousandsSeparator)
            .HasMaxLength(1)
            .HasConversion(c => c.ToString(), s => s[0])
            .IsRequired();

        builder.Property(cf => cf.DecimalPlaces).IsRequired();
        builder.Property(cf => cf.NegativePattern).HasMaxLength(30).IsRequired();

        // Composite unique index: one format per language + currency pair
        builder.HasIndex(cf => new { cf.LanguageCode, cf.CurrencyCode })
            .IsUnique()
            .HasDatabaseName("UX_CurrencyFormats_Language_Currency");

        // Audit fields
        builder.Property(cf => cf.CreatedAt).IsRequired();
        builder.Property(cf => cf.CreatedBy).HasMaxLength(256).IsRequired();
        builder.Property(cf => cf.UpdatedAt);
        builder.Property(cf => cf.UpdatedBy).HasMaxLength(256);
    }
}
