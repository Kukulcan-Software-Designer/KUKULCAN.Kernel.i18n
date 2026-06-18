namespace KUKULCAN.Kernel.i18n.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="Language"/>.
/// Maps the global language catalogue to the <c>i18n.Languages</c> table.
/// </summary>
public sealed class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    /// <summary>
    /// Configures the entity type for <see cref="Language"/>.
    /// </summary>
    /// <param name="builder">The builder to configure the entity type.</param>
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
