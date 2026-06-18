using MediatR;
using Microsoft.Extensions.Options;

namespace KUKULCAN.Kernel.i18n.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the KUKULCAN.Kernel.i18n module.
///
/// <para>
/// Extends <see cref="KukulcanDbContextBase"/> from
/// <c>ATLAS.Kernel.Database</c>, which wires automatically:
/// <list type="bullet">
///   <item>Audit fields via <c>AuditSaveChangesInterceptor</c>.</item>
///   <item>Soft-delete conversion via <c>SoftDeleteInterceptor</c>.</item>
///   <item>Domain event dispatch via <c>DomainEventDispatchInterceptor</c>.</item>
///   <item>Immutable entity enforcement via <c>ImmutableEntityInterceptor</c>.</item>
///   <item>Slow query logging via <c>SlowQueryInterceptor</c>.</item>
///   <item>All <c>IEntityTypeConfiguration&lt;T&gt;</c> found in this assembly.</item>
///   <item>Global soft-delete query filter for <c>ISoftDeletable</c> entities.</item>
/// </list>
/// </para>
///
/// <para>
/// <b>Multi-tenancy note:</b> i18n data (languages, translations, locale configs,
/// currency formats) is <b>global</b> — shared across all tenants.
/// Therefore <c>ITenantAware</c> is not implemented by any i18n entity and the
/// tenant filter is never applied. The base class handles this correctly because
/// <c>ApplyTenantFilter</c> only filters entities that implement <c>ITenantAware</c>.
/// </para>
///
/// <para>
/// Schema: <c>i18n</c> — isolated within the shared ATLAS database (or a dedicated DB).
/// </para>
/// </summary>
/// <param name="options">The options parameter.</param>
/// <param name="tenantContext">The tenantContext parameter.</param>
/// <param name="currentUser">The currentUser parameter.</param>
/// <param name="dateTimeProvider">The dateTimeProvider parameter.</param>
/// <param name="publisher">The publisher parameter.</param>
public sealed class I18NDbContext(IOptions<KukulcanDatabaseOptions> options, ITenantContext tenantContext, ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider, IPublisher publisher) : KukulcanDbContextBase(options, tenantContext, currentUser, dateTimeProvider, publisher)
{

    // ── DbSets ────────────────────────────────────────────────────────────────
    /// <summary>
    /// Executes this member.
    /// </summary>
    public DbSet<Language> Languages => Set<Language>();
    /// <summary>
    /// Executes this member.
    /// </summary>
    public DbSet<Translation> Translations => Set<Translation>();
    /// <summary>
    /// Executes this member.
    /// </summary>
    public DbSet<LocaleConfiguration> LocaleConfigurations => Set<LocaleConfiguration>();
    /// <summary>
    /// Executes this member.
    /// </summary>
    public DbSet<CurrencyFormat> CurrencyFormats => Set<CurrencyFormat>();

    // ── Model configuration ───────────────────────────────────────────────────
    /// <summary>
    /// Executes OnModelCreating.
    /// </summary>
    /// <param name="modelBuilder">The modelBuilder parameter.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Set default schema for all i18n tables
        modelBuilder.HasDefaultSchema("i18n");

        // Base class applies:
        //   - ApplyConfigurationsFromAssembly(GetType().Assembly)  → picks up all IEntityTypeConfiguration<T>
        //   - ApplySoftDeleteFilter()                              → WHERE IsDeleted = 0 (no i18n entities are soft-deletable, no-op)
        //   - ApplyTenantFilter(_tenantContext)                    → WHERE TenantId = @current (no i18n entities are ITenantAware, no-op)
        base.OnModelCreating(modelBuilder);
    }
}
