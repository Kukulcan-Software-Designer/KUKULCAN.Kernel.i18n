using KUKULCAN.Kernel.Database.Extensions;
using KUKULCAN.Kernel.i18n.Domain.Services;
using KUKULCAN.Kernel.i18n.Infrastructure.Persistence;
using KUKULCAN.Kernel.i18n.Infrastructure.Persistence.Repositories;
using KUKULCAN.Kernel.i18n.Infrastructure.Persistence.Seeds;
using KUKULCAN.Kernel.i18n.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KUKULCAN.Kernel.i18n.Infrastructure;

/// <summary>
/// Registers all Infrastructure layer services for ATLAS.i18n.
/// Call from <c>Program.cs</c>: <c>services.AddAtlasI18nInfrastructure(configuration);</c>
/// </summary>
public static class InfrastructureServiceRegistration
{
    /// <summary>
    /// Registers all services related to the Infrastructure layer, including:
    ///   - DbContext and Unit of Work
    ///   - Repositories
    ///   - System services (current user, tenant context, date/time provider)
    ///   - Caching (Redis or in-memory fallback)
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection AddKukulcanI18NInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ── DbContext + IUnitOfWork (via SharedKernel extension) ───────────────
        //
        // AddKukulcanDbContext<T> does all of:
        //   ① Binds KukulcanDatabaseOptions from "Kukulcan:Database" config section
        //   ② Registers I18nDbContext (scoped)
        //   ③ Registers IUnitOfWork → UnitOfWork<I18nDbContext> (scoped)
        //   ④ Registers SlowQueryInterceptor (singleton)
        //
        services.AddKukulcanDbContext<I18NDbContext>(configuration);

        // ── Repositories ──────────────────────────────────────────────────────
        services.AddScoped<ILanguageRepository, LanguageRepository>();
        services.AddScoped<ITranslationRepository, TranslationRepository>();
        services.AddScoped<ILocaleConfigurationRepository, LocaleConfigurationRepository>();
        services.AddScoped<ICurrencyFormatRepository, CurrencyFormatRepository>();

        // ── System services (ICurrentUser, ITenantContext, IDateTimeProvider) ──
        // i18n is a global service — no real tenant context needed
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddSingleton<ITenantContext, I18NSystemTenantContext>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // ── Cache (ICacheService from SharedKernel.Abstractions) ──────────────
        RegisterCacheService(services, configuration);

        return services;
    }

    /// <summary>
    /// Applies EF Core migrations and seeds baseline data.
    /// Called once during application startup after the host is built.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="ct"></param>
    public static async Task MigrateAndSeedAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<I18NDbContext>();

        await ctx.Database.MigrateAsync(ct);
        await I18NSeedData.SeedAsync(ctx, ct);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static void RegisterCacheService(IServiceCollection services, IConfiguration configuration)
    {
        string? redisConnection = configuration.GetConnectionString("Redis");

        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddStackExchangeRedisCache(opts =>
            {
                opts.Configuration = redisConnection;
                opts.InstanceName = "KUKULCAN.Kernel.i18n:";
            });
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, DistributedCacheService>();
        }
        else
        {
            // Fallback: pure in-memory cache (development / single-node)
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, MemoryOnlyCacheService>();
        }
    }
}
