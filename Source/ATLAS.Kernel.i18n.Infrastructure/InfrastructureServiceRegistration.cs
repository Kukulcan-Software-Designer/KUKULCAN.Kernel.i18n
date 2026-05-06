using ATLAS.Kernel.Database.Extensions;
using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;
using ATLAS.Kernel.i18n.Infrastructure.Caching;
using ATLAS.Kernel.i18n.Infrastructure.Persistence;
using ATLAS.Kernel.i18n.Infrastructure.Persistence.Repositories;
using ATLAS.Kernel.i18n.Infrastructure.Persistence.Seeds;
using ATLAS.Kernel.i18n.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ATLAS.Kernel.i18n.Infrastructure;

/// <summary>
/// Registers all Infrastructure layer services for ATLAS.i18n.
/// Call from <c>Program.cs</c>: <c>services.AddAtlasI18nInfrastructure(configuration);</c>
/// </summary>
public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddAtlasI18nInfrastructure(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        // ── DbContext + IUnitOfWork (via SharedKernel extension) ───────────────
        //
        // AddAtlasDbContext<T> does all of:
        //   ① Binds AtlasDatabaseOptions from "Atlas:Database" config section
        //   ② Registers I18nDbContext (scoped)
        //   ③ Registers IUnitOfWork → UnitOfWork<I18nDbContext> (scoped)
        //   ④ Registers SlowQueryInterceptor (singleton)
        //
        services.AddAtlasDbContext<I18nDbContext>(configuration);

        // ── Repositories ──────────────────────────────────────────────────────
        services.AddScoped<ILanguageRepository,           LanguageRepository>();
        services.AddScoped<ITranslationRepository,        TranslationRepository>();
        services.AddScoped<ILocaleConfigurationRepository, LocaleConfigurationRepository>();
        services.AddScoped<ICurrencyFormatRepository,     CurrencyFormatRepository>();

        // ── System services (ICurrentUser, ITenantContext, IDateTimeProvider) ──
        // i18n is a global service — no real tenant context needed
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser,      HttpCurrentUser>();
        services.AddSingleton<ITenantContext, I18nSystemTenantContext>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // ── Cache (ICacheService from SharedKernel.Abstractions) ──────────────
        RegisterCacheService(services, configuration);

        return services;
    }

    /// <summary>
    /// Applies EF Core migrations and seeds baseline data.
    /// Called once during application startup after the host is built.
    /// </summary>
    public static async Task MigrateAndSeedAsync(
        IServiceProvider  serviceProvider,
        CancellationToken ct = default)
    {
        using var scope = serviceProvider.CreateScope();
        var ctx         = scope.ServiceProvider.GetRequiredService<I18nDbContext>();

        await ctx.Database.MigrateAsync(ct);
        await I18nSeedData.SeedAsync(ctx, ct);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static void RegisterCacheService(
        IServiceCollection services,
        IConfiguration     configuration)
    {
        var redisConnection = configuration.GetConnectionString("Redis");

        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddStackExchangeRedisCache(opts =>
            {
                opts.Configuration = redisConnection;
                opts.InstanceName   = "ATLAS.i18n:";
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
