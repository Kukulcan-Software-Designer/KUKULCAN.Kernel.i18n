using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ATLAS.Kernel.i18n.API.Extensions;

public static class ApiServiceExtensions
{
    public static IServiceCollection AddAtlasI18nApi(this IServiceCollection services, IConfiguration configuration)
    {
        // ── Controllers ───────────────────────────────────────────────────────
        services.AddControllers()
            .AddJsonOptions(opts =>
            {
                opts.JsonSerializerOptions.PropertyNamingPolicy        =
                    System.Text.Json.JsonNamingPolicy.CamelCase;
                opts.JsonSerializerOptions.DefaultIgnoreCondition      =
                    System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        // ── OpenAPI / Scalar ──────────────────────────────────────────────────
        services.AddOpenApi(opts =>
        {
            opts.AddDocumentTransformer((doc, _, _) =>
            {
                doc.Info.Title       = "ATLAS.Kernel.i18n — Internationalisation Service";
                doc.Info.Version     = "v1";
                doc.Info.Description =
                    "Global translation lookup, locale configuration, and currency formatting for ATLAS ERP. " +
                    "All data is global (not tenant-scoped). " +
                    "Translations use BCP-47 language tags and fall back automatically via the language chain " +
                    "(e.g. es-MX → es → en).";
                return Task.CompletedTask;
            });
        });

        // ── JWT Bearer (shared with the rest of ATLAS) ────────────────────────
        var jwtSection = configuration.GetSection("Jwt");
        var key        = Encoding.UTF8.GetBytes(
            jwtSection["SecretKey"] ?? "ATLAS_DEFAULT_DEV_KEY_CHANGE_IN_PROD_MIN_32CH");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = jwtSection["Issuer"]   ?? "ATLAS",
                    ValidAudience            = jwtSection["Audience"] ?? "ATLAS.Kernel.i18n",
                    IssuerSigningKey         = new SymmetricSecurityKey(key),
                    ClockSkew                = TimeSpan.FromMinutes(5),
                };
            });

        // ── Authorization policies ────────────────────────────────────────────
        services.AddAuthorization(opts =>
        {
            // Any authenticated ATLAS user may read translations
            opts.AddPolicy("i18n.read",  policy => policy.RequireAuthenticatedUser());
            // Only ATLAS admins may write (create/update/delete)
            opts.AddPolicy("i18n.write", policy =>
                policy.RequireRole("ATLAS.Admin", "ATLAS.i18n.Admin"));
        });

        // ── Health checks ─────────────────────────────────────────────────────
        var connStr = configuration.GetConnectionString("Database") ?? string.Empty;
        var redis   = configuration.GetConnectionString("Redis")    ?? string.Empty;

        var hc = services.AddHealthChecks()
            .AddCheck("self",
                () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(),
                tags: ["live"]);

        if (!string.IsNullOrWhiteSpace(connStr))
            hc.AddNpgSql(connStr, name: "postgresql", tags: ["ready", "db"]);

        if (!string.IsNullOrWhiteSpace(redis))
            hc.AddRedis(redis, name: "redis", tags: ["ready", "cache"]);

        return services;
    }
}
