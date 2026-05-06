namespace ATLAS.Kernel.i18n.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="CurrencyFormat"/>.
/// Maps currency formatting rules to the <c>i18n.CurrencyFormats</c> table.
/// </summary>
public sealed class CurrencyFormatConfiguration : IEntityTypeConfiguration<CurrencyFormat>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
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
