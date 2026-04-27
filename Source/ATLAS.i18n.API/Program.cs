using ATLAS.i18n.API.Extensions;
using ATLAS.i18n.API.Middleware;
using ATLAS.i18n.Application;
using ATLAS.i18n.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

// ─── Bootstrap Logger (pre-DI) ────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting ATLAS.i18n Service...");

    var builder = WebApplication.CreateBuilder(args);

    // ─── Serilog ──────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext()
              .Enrich.WithMachineName()
              .Enrich.WithEnvironmentName()
              .Enrich.WithProcessId()
              .Enrich.WithThreadId()
              .WriteTo.Console(
                  outputTemplate:
                  "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"));

    // ─── Application & Infrastructure layers ──────────────────────────────────
    builder.Services.AddAtlasI18nApplication();
    builder.Services.AddAtlasI18nInfrastructure(builder.Configuration);

    // ─── API layer ────────────────────────────────────────────────────────────
    builder.Services.AddAtlasI18nApi(builder.Configuration);

    // ─── Windows Service support ──────────────────────────────────────────────
    builder.Host.UseWindowsService(opts =>
        opts.ServiceName = "ATLAS.i18n");

    // ─── Systemd (Linux) service support ─────────────────────────────────────
    builder.Host.UseSystemd();

    // ─────────────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ─── Database migration & seed ────────────────────────────────────────────
    if (app.Configuration.GetValue<bool>("Database:AutoMigrate", defaultValue: true))
    {
        Log.Information("Applying database migrations...");
        await InfrastructureServiceRegistration.MigrateAndSeedAsync(app.Services);
        Log.Information("Migrations complete.");
    }

    // ─── Middleware pipeline ──────────────────────────────────────────────────
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(opts =>
        {
            opts.Title          = "ATLAS.i18n API";
            opts.Theme          = Scalar.AspNetCore.ScalarTheme.Purple;
            opts.DefaultHttpClient = (Scalar.AspNetCore.ScalarTarget.CSharp,
                                      Scalar.AspNetCore.ScalarClient.HttpClient);
        });
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => true });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = hc => hc.Tags.Contains("ready")
    });
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = hc => hc.Tags.Contains("live")
    });

    Log.Information("ATLAS.i18n Service ready on {Urls}",
        string.Join(", ", app.Urls));

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "ATLAS.i18n Service terminated unexpectedly.");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;
