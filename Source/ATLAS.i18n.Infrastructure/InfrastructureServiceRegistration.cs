using ATLAS.i18n.Application.Common.Interfaces;
using ATLAS.i18n.Domain.Repositories;
using ATLAS.i18n.Infrastructure.Caching;
using ATLAS.i18n.Infrastructure.Persistence;
using ATLAS.i18n.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ATLAS.i18n.Infrastructure;

/// <summary>
/// Registers all Infrastructure layer services: EF Core, caching, and repositories.
/// </summary>
public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddAtlasI18nInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ─── Database ─────────────────────────────────────────────────────────
        var connectionString = configuration.GetConnectionString("I18nDb")
            ?? throw new InvalidOperationException(
                "Connection string 'I18nDb' is not configured. " +
                "Add ConnectionStrings:I18nDb to appsettings.json.");

        services.AddDbContext<I18nDbContext>(options =>
        {
            // Default: PostgreSQL — switch provider here if needed
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "i18n");
                npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });

            // Enable detailed errors and sensitive data logging in Development only
            // (controlled externally via environment)
        });

        // ─── Repositories ──────────────────────────────────────────────────────
        services.AddScoped<ILanguageRepository,           LanguageRepository>();
        services.AddScoped<ITranslationRepository,        TranslationRepository>();
        services.AddScoped<ILocaleConfigurationRepository, LocaleConfigurationRepository>();
        services.AddScoped<ICurrencyFormatRepository,     CurrencyFormatRepository>();
        services.AddScoped<IUnitOfWork,                   UnitOfWork>();

        // ─── Caching ──────────────────────────────────────────────────────────
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
            // Fallback: pure in-memory cache (single-node / development)
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, MemoryCacheService>();
        }

        return services;
    }

    /// <summary>
    /// Applies pending EF Core migrations and runs the seed data.
    /// Call during application startup (after building the host).
    /// </summary>
    public static async Task MigrateAndSeedAsync(IServiceProvider serviceProvider)
    {
        using var scope   = serviceProvider.CreateScope();
        var context       = scope.ServiceProvider.GetRequiredService<I18nDbContext>();

        await context.Database.MigrateAsync();
        await Seeds.I18nSeedData.SeedAsync(context);
    }
}
