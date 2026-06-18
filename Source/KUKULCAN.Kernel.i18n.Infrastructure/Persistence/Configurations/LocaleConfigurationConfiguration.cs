namespace KUKULCAN.Kernel.i18n.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="LocaleConfiguration"/>.
/// Maps locale formatting rules to the <c>i18n.LocaleConfigurations</c> table.
/// </summary>
public sealed class LocaleConfigurationConfiguration1 : IEntityTypeConfiguration<LocaleConfiguration>
{
    /// <summary>
    /// Configures the entity type for <see cref="LocaleConfiguration"/>.
    /// </summary>
    /// <param name="builder">The builder to configure the entity type.</param>
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
