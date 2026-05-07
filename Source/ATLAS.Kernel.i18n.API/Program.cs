using ATLAS.Kernel.i18n.API.Startup;
using ATLAS.Kernel.i18n.Infrastructure;
using Microsoft.Extensions.Hosting;
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

    AppStartup.ConfigureServices(builder);

    // ──────────────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── Migration + Seed ───────────────────────────────────────────────────────
    if (app.Configuration.GetValue("Database:AutoMigrate", defaultValue: false))
    {
        Log.Information("Applying database migrations…");
        await InfrastructureServiceRegistration.MigrateAndSeedAsync(app.Services);
        Log.Information("Migrations applied.");
    }

    app.UseSerilogRequestLogging(opts =>
        opts.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.000}ms");
    AppStartup.ConfigurePipeline(app);

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
