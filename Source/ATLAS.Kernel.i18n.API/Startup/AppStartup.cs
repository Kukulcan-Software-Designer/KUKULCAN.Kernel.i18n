using ATLAS.Kernel.i18n.API.Extensions;
using ATLAS.Kernel.i18n.API.Middleware;
using ATLAS.Kernel.i18n.Application;
using ATLAS.Kernel.i18n.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

namespace ATLAS.Kernel.i18n.API.Startup;

public static class AppStartup
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddAtlasI18NApplication();
        builder.Services.AddAtlasI18NInfrastructure(builder.Configuration);
        builder.Services.AddAtlasI18NApi(builder.Configuration);
    }

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

