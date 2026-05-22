namespace ATLAS.Kernel.i18n.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="Translation"/>.
/// Maps translation entries to the <c>i18n.Translations</c> table.
/// </summary>
public sealed class TranslationConfiguration : IEntityTypeConfiguration<Translation>
{
    /// <summary>
    /// Configures the entity type for <see cref="Translation"/>.
    /// </summary>
    /// <param name="builder">The builder to configure the entity type.</param>
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
