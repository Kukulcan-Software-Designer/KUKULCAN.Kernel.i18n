using ATLAS.SharedKernel.Database;
using ATLAS.SharedKernel.Database.Configuration;
using MediatR;
using Microsoft.Extensions.Options;

namespace ATLAS.i18n.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the ATLAS.i18n module.
///
/// <para>
/// Extends <see cref="AtlasDbContextBase"/> from
/// <c>ATLAS.SharedKernel.Database</c>, which wires automatically:
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
public sealed class I18nDbContext : AtlasDbContextBase
{
    public I18nDbContext(
        IOptions<AtlasDatabaseOptions> options,
        ITenantContext                 tenantContext,
        ICurrentUser                  currentUser,
        IDateTimeProvider             dateTimeProvider,
        IPublisher                    publisher)
        : base(options, tenantContext, currentUser, dateTimeProvider, publisher)
    { }

    // ── DbSets ────────────────────────────────────────────────────────────────

    public DbSet<Language>            Languages            => Set<Language>();
    public DbSet<Translation>         Translations         => Set<Translation>();
    public DbSet<LocaleConfiguration> LocaleConfigurations => Set<LocaleConfiguration>();
    public DbSet<CurrencyFormat>      CurrencyFormats      => Set<CurrencyFormat>();

    // ── Model configuration ───────────────────────────────────────────────────

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
