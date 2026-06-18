using KUKULCAN.Kernel.i18n.Client.ApiClient;
using KUKULCAN.Kernel.i18n.Client.Configuration;
using KUKULCAN.Kernel.i18n.Client.UI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

// ── Configuration ─────────────────────────────────────────────────────────────
IConfigurationRoot configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

ApiSettings apiSettings = configuration.GetSection(ApiSettings.SectionKey).Get<ApiSettings>()
                          ?? throw new InvalidOperationException("Falta la sección 'Api' en appsettings.json");
AtlasDatabaseSettings dbSettings = configuration.GetSection(AtlasDatabaseSettings.SectionKey).Get<AtlasDatabaseSettings>()
                                   ?? throw new InvalidOperationException("Falta la sección 'Atlas:Database' en appsettings.json");

ValidateDatabaseSettings(dbSettings);

// ── DI container ──────────────────────────────────────────────────────────────
var services = new ServiceCollection();

services.AddSingleton(apiSettings);
services.AddSingleton(dbSettings);

services.AddHttpClient<I18NApiClient>(client =>
{
    client.BaseAddress = new Uri(apiSettings.BaseUrl.TrimEnd('/') + "/");

    if (!string.IsNullOrWhiteSpace(apiSettings.BearerToken))
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiSettings.BearerToken);

    client.Timeout = TimeSpan.FromSeconds(30);
});

services.AddTransient<ConsoleMenu>();

var sp = services.BuildServiceProvider();

// ── Run ───────────────────────────────────────────────────────────────────────
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try
{
    var menu = sp.GetRequiredService<ConsoleMenu>();
    await menu.RunAsync(cts.Token);
}
catch (OperationCanceledException)
{
    AnsiConsole.MarkupLine("[grey]Operación cancelada.[/]");
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths);
    return 1;
}

AnsiConsole.MarkupLine("[grey]¡Hasta luego![/]");
return 0;

// ── Validation ────────────────────────────────────────────────────────────────
static void ValidateDatabaseSettings(AtlasDatabaseSettings db)
{
    if (!Enum.IsDefined(db.Provider))
    {
        AnsiConsole.MarkupLine(
            "[red]✘ Proveedor de base de datos no válido.[/]\n" +
            "Valores permitidos en appsettings.json → Atlas:Database:Provider:\n" +
            "  [cyan]PostgreSql[/] | [red]SqlServer[/] | [orange1]MySql[/]");
        Environment.Exit(1);
    }

    if (string.IsNullOrWhiteSpace(db.ConnectionString))
    {
        AnsiConsole.MarkupLine(
            "[red]✘ La cadena de conexión está vacía.[/]\n" +
            "Define [bold]Atlas:Database:ConnectionString[/] en appsettings.json.");
        Environment.Exit(1);
    }
}
