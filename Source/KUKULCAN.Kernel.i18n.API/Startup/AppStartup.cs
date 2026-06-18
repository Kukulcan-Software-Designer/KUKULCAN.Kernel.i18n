using KUKULCAN.Kernel.i18n.Application;
using KUKULCAN.Kernel.i18n.API.Extensions;
using KUKULCAN.Kernel.i18n.API.Middleware;
using KUKULCAN.Kernel.i18n.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

namespace KUKULCAN.Kernel.i18n.API.Startup;

/// <summary>
///
/// </summary>
public static class AppStartup
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="builder"></param>
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddKukulcanI18NApplication();
        builder.Services.AddKukulcanI18NInfrastructure(builder.Configuration);
        builder.Services.AddKukulcanI18NApi(builder.Configuration);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="app"></param>
    public static void ConfigurePipeline(WebApplication app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => true });
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = hc => hc.Tags.Contains("live") });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = hc => hc.Tags.Contains("ready") });

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(opts =>
            {
                opts.Title = "ATLAS.Kernel.i18n";
                opts.Theme = ScalarTheme.Purple;
                opts.DefaultHttpClient = new KeyValuePair<ScalarTarget, ScalarClient>(ScalarTarget.CSharp, ScalarClient.HttpClient);
            });
        }
    }
}

