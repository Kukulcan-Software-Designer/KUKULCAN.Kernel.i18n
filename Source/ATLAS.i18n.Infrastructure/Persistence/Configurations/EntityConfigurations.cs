using ATLAS.i18n.Domain.Entities;
using ATLAS.i18n.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATLAS.i18n.Infrastructure.Persistence.Configurations;

// ─── Language ─────────────────────────────────────────────────────────────────

public sealed class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.ToTable("Languages");

        // PK is LanguageCode (value object wrapping string)
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .HasColumnName("Code")
            .HasMaxLength(2)
            .HasConversion(
                v => v.Value,
                v => LanguageCode.From(v))
            .IsRequired();

        builder.Property(l => l.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(l => l.NativeName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(l => l.CultureTag)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(l => l.IsDefault).IsRequired();
        builder.Property(l => l.IsActive).IsRequired();

        builder.Property(l => l.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(l => l.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(l => l.IsDefault)
            .HasFilter("\"IsDefault\" = true")
            .HasDatabaseName("IX_Languages_Default");

        builder.HasIndex(l => l.IsActive)
            .HasDatabaseName("IX_Languages_Active");

        // Navigation — LocaleConfiguration (one-to-one, owned by Language aggregate)
        builder.HasOne(l => l.LocaleConfiguration)
            .WithOne()
            .HasForeignKey<LocaleConfiguration>("LanguageCode")
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation — CurrencyFormats (one-to-many, owned by Language aggregate)
        builder.HasMany(l => l.CurrencyFormats)
            .WithOne()
            .HasForeignKey("LanguageCode")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// ─── Translation ──────────────────────────────────────────────────────────────

public sealed class TranslationConfiguration : IEntityTypeConfiguration<Translation>
{
    public void Configure(EntityTypeBuilder<Translation> builder)
    {
        builder.ToTable("Translations");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Code)
            .HasColumnName("Code")
            .HasMaxLength(9)
            .HasConversion(
                v => v.Value,
                v => TranslationCode.From(v))
            .IsRequired();

        builder.Property(t => t.LanguageCode)
            .HasColumnName("LanguageCode")
            .HasMaxLength(2)
            .HasConversion(
                v => v.Value,
                v => LanguageCode.From(v))
            .IsRequired();

        builder.Property(t => t.Text)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(t => t.Context)
            .HasMaxLength(500);

        builder.Property(t => t.MaxLength);
        builder.Property(t => t.IsReviewed).IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(t => t.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Composite unique index: Code + LanguageCode is the natural business key
        builder.HasIndex(t => new { t.Code, t.LanguageCode })
            .IsUnique()
            .HasDatabaseName("UX_Translations_Code_Language");

        // Index for module queries: filter all CRM, PIM, etc. entries efficiently
        // We store the module prefix in a computed column shadow property
        builder.HasIndex(t => t.LanguageCode)
            .HasDatabaseName("IX_Translations_LanguageCode");

        // Foreign key to Language (soft reference — language must exist)
        builder.HasOne<Language>()
            .WithMany()
            .HasForeignKey(t => t.LanguageCode)
            .HasPrincipalKey(l => l.Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// ─── LocaleConfiguration ─────────────────────────────────────────────────────

public sealed class LocaleConfigurationConfiguration
    : IEntityTypeConfiguration<LocaleConfiguration>
{
    public void Configure(EntityTypeBuilder<LocaleConfiguration> builder)
    {
        builder.ToTable("LocaleConfigurations");

        builder.HasKey(lc => lc.Id);
        builder.Property(lc => lc.Id).ValueGeneratedNever();

        builder.Property(lc => lc.LanguageCode)
            .HasColumnName("LanguageCode")
            .HasMaxLength(2)
            .HasConversion(
                v => v.Value,
                v => LanguageCode.From(v))
            .IsRequired();

        builder.Property(lc => lc.DateFormat).HasMaxLength(50).IsRequired();
        builder.Property(lc => lc.ShortDateFormat).HasMaxLength(50).IsRequired();
        builder.Property(lc => lc.TimeFormat).HasMaxLength(50).IsRequired();
        builder.Property(lc => lc.DateTimeFormat).HasMaxLength(100).IsRequired();
        builder.Property(lc => lc.FirstDayOfWeek).IsRequired();

        // Store separator chars as single-character strings
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

        builder.Property(lc => lc.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(lc => lc.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(lc => lc.LanguageCode)
            .IsUnique()
            .HasDatabaseName("UX_LocaleConfigurations_LanguageCode");
    }
}

// ─── CurrencyFormat ───────────────────────────────────────────────────────────

public sealed class CurrencyFormatConfiguration : IEntityTypeConfiguration<CurrencyFormat>
{
    public void Configure(EntityTypeBuilder<CurrencyFormat> builder)
    {
        builder.ToTable("CurrencyFormats");

        builder.HasKey(cf => cf.Id);
        builder.Property(cf => cf.Id).ValueGeneratedNever();

        builder.Property(cf => cf.LanguageCode)
            .HasColumnName("LanguageCode")
            .HasMaxLength(2)
            .HasConversion(
                v => v.Value,
                v => LanguageCode.From(v))
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

        // Composite unique index: one format per language+currency pair
        builder.HasIndex(cf => new { cf.LanguageCode, cf.CurrencyCode })
            .IsUnique()
            .HasDatabaseName("UX_CurrencyFormats_Language_Currency");

        // FK to Language
        builder.HasOne<Language>()
            .WithMany()
            .HasForeignKey(cf => cf.LanguageCode)
            .HasPrincipalKey(l => l.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
