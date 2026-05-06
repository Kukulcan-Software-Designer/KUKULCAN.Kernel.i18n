using ATLAS.Kernel.i18n.API.Extensions;
using ATLAS.Kernel.i18n.API.Middleware;
using ATLAS.Kernel.i18n.Application;
using ATLAS.Kernel.i18n.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;

// ── Bootstrap logger (before DI is built) ────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting ATLAS.Kernel.i18n Service…");

    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ────────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, svc, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(svc)
           .Enrich.FromLogContext()
           .Enrich.WithMachineName()
           .Enrich.WithEnvironmentName()
           .WriteTo.Console(
               outputTemplate:
               "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"));

    // ── Windows Service + Linux systemd ────────────────────────────────────────
    builder.Host.UseWindowsService(opts => opts.ServiceName = "ATLAS.Kernel.i18n");
    builder.Host.UseSystemd();

    // ── Application + Infrastructure layers ───────────────────────────────────
    builder.Services.AddAtlasI18nApplication();
    builder.Services.AddAtlasI18nInfrastructure(builder.Configuration);

    // ── API layer ──────────────────────────────────────────────────────────────
    builder.Services.AddAtlasI18nApi(builder.Configuration);

    // ──────────────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── Migration + Seed ───────────────────────────────────────────────────────
    if (app.Configuration.GetValue("Database:AutoMigrate", defaultValue: false))
    {
        Log.Information("Applying database migrations…");
        await InfrastructureServiceRegistration.MigrateAndSeedAsync(app.Services);
        Log.Information("Migrations applied.");
    }

    // ── Middleware pipeline ────────────────────────────────────────────────────
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseSerilogRequestLogging(opts =>
        opts.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.000}ms");

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(opts =>
        {
            opts.Title             = "ATLAS.Kernel.i18n";
            opts.Theme             = Scalar.AspNetCore.ScalarTheme.Purple;
            opts.DefaultHttpClient = (ScalarTarget.CSharp, ScalarClient.HttpClient);
        });
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapHealthChecks("/health",       new HealthCheckOptions { Predicate = _ => true });
    app.MapHealthChecks("/health/live",  new HealthCheckOptions { Predicate = hc => hc.Tags.Contains("live") });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = hc => hc.Tags.Contains("ready") });

    Log.Information("ATLAS.Kernel.i18n ready on {Urls}", string.Join(", ", app.Urls));

    await app.RunAsync();
    return 0;
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "ATLAS.Kernel.i18n terminated unexpectedly.");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
