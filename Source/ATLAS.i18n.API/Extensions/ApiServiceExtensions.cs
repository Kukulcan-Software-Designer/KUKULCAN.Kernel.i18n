using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ATLAS.i18n.API.Extensions;

public static class ApiServiceExtensions
{
    public static IServiceCollection AddAtlasI18nApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers(opts =>
        {
            opts.SuppressAsyncSuffixInActionNames = false;
        })
        .AddJsonOptions(opts =>
        {
            opts.JsonSerializerOptions.PropertyNamingPolicy        =
                System.Text.Json.JsonNamingPolicy.CamelCase;
            opts.JsonSerializerOptions.DefaultIgnoreCondition      =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        });

        // OpenAPI / Scalar
        services.AddOpenApi(opts =>
        {
            opts.AddDocumentTransformer((doc, ctx, ct) =>
            {
                doc.Info.Title       = "ATLAS.i18n API";
                doc.Info.Version     = "v1";
                doc.Info.Description =
                    "Internationalisation service for the ATLAS platform. " +
                    "Provides translations, locale configurations, and currency formatting.";
                return Task.CompletedTask;
            });
        });

        // JWT Authentication (shared with the rest of ATLAS)
        var jwtSection = configuration.GetSection("Jwt");
        var key        = Encoding.UTF8.GetBytes(
            jwtSection["SecretKey"] ?? "ATLAS_DEFAULT_DEV_KEY_CHANGE_IN_PRODUCTION");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = jwtSection["Issuer"]   ?? "ATLAS",
                    ValidAudience            = jwtSection["Audience"] ?? "ATLAS.i18n",
                    IssuerSigningKey         = new SymmetricSecurityKey(key),
                    ClockSkew                = TimeSpan.FromMinutes(5),
                };
            });

        services.AddAuthorization(opts =>
        {
            // Read-only policy — any authenticated user can query translations
            opts.AddPolicy("i18n.read", policy =>
                policy.RequireAuthenticatedUser());

            // Write policy — restricted to admin role
            opts.AddPolicy("i18n.write", policy =>
                policy.RequireRole("ATLAS.Admin", "ATLAS.i18n.Admin"));
        });

        // Health checks
        var connectionString = configuration.GetConnectionString("I18nDb") ?? string.Empty;
        var redisConnection  = configuration.GetConnectionString("Redis")   ?? string.Empty;

        var hcBuilder = services.AddHealthChecks()
            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(),
                tags: ["live"]);

        if (!string.IsNullOrWhiteSpace(connectionString))
            hcBuilder.AddNpgSql(
                connectionString,
                name:  "postgresql",
                tags:  ["ready", "db"]);

        if (!string.IsNullOrWhiteSpace(redisConnection))
            hcBuilder.AddRedis(
                redisConnection,
                name:  "redis",
                tags:  ["ready", "cache"]);

        return services;
    }
}
