using ATLAS.Kernel.i18n.Client.ApiClient;
using ATLAS.Kernel.i18n.Client.Configuration;
using ATLAS.Kernel.i18n.Client.Models;
using Spectre.Console;
using System.Text.Json;

namespace ATLAS.Kernel.i18n.Client.UI;

/// <summary>
/// Drives the interactive console. Renders menus via Spectre.Console and
/// dispatches actions to <see cref="I18NApiClient"/>.
/// </summary>
public sealed class ConsoleMenu(I18NApiClient api, ApiSettings apiSettings, AtlasDatabaseSettings dbSettings)
{
    private static readonly JsonSerializerOptions _prettyJson = new() { WriteIndented = true };

    // Entry point

    public async Task RunAsync(CancellationToken ct)
    {
        PrintBanner();

        while (!ct.IsCancellationRequested)
        {
            string section = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]── ATLAS i18n Client ──[/]  Elige una sección:")
                    .AddChoices(
                        "🌐  Languages   — Gestión de idiomas",
                        "🗓️  Locales     — Formatos de fecha/número",
                        "💱  Currencies  — Formatos de moneda",
                        "🔤  Translations— Traducciones",
                        "⚙️  Configuración",
                        "❌  Salir"));

            if (section.StartsWith("❌"))
                break;

            // Emoji fuera del BMP (> U+FFFF) no pueden ser char literals en C#.
            // Se usa Contains sobre el texto de la opción seleccionada.
            await (section switch
            {
                var s when s.Contains("Languages")     => LanguagesMenuAsync(ct),
                var s when s.Contains("Locales")       => LocalesMenuAsync(ct),
                var s when s.Contains("Currencies")    => CurrenciesMenuAsync(ct),
                var s when s.Contains("Translations")  => TranslationsMenuAsync(ct),
                var s when s.Contains("Configuración") => ConfigMenuAsync(),
                _ => Task.CompletedTask
            });
        }
    }

    // Banner

    private void PrintBanner()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[bold blue]ATLAS.Kernel.i18n — Cliente de consola[/]").RuleStyle(Style.Parse("blue")));

        string providerColor = dbSettings.Provider switch
        {
            DatabaseProvider.PostgreSql => "cyan",
            DatabaseProvider.SqlServer  => "red",
            DatabaseProvider.MySql      => "orange1",
            _                           => "white"
        };

        Grid grid = new Grid().AddColumn().AddColumn();
        grid.AddRow("[grey]API Base URL:[/]",  $"[green]{apiSettings.BaseUrl}[/]");
        grid.AddRow("[grey]DB Provider:[/]",   $"[{providerColor}]{dbSettings.Provider}[/]");
        grid.AddRow("[grey]Auth Token:[/]",
            string.IsNullOrWhiteSpace(apiSettings.BearerToken) ? "[red]⚠ No configurado[/]" : "[green]✔ Configurado[/]");
        AnsiConsole.Write(grid);
        AnsiConsole.WriteLine();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // LANGUAGES
    // ══════════════════════════════════════════════════════════════════════════

    private async Task LanguagesMenuAsync(CancellationToken ct)
    {
        string action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[blue]Languages[/] — Acción:")
                .AddChoices(
                    "Listar todos",
                    "Obtener por código",
                    "Crear idioma",
                    "Actualizar idioma",
                    "Activar / Desactivar",
                    "Establecer como predeterminado",
                    "← Volver"));

        switch (action)
        {
            case "Listar todos":
                bool activeOnly = AnsiConsole.Confirm("¿Solo idiomas activos?", defaultValue: true);
                await ExecuteAsync(() => api.GetAllLanguagesAsync(activeOnly, ct), PrintLanguages);
                break;

            case "Obtener por código":
                string code = Prompt("Código BCP-47 (ej: es-ES)");
                await ExecuteAsync(() => api.GetLanguageAsync(code, ct), l => PrintLanguages([l]));
                break;

            case "Crear idioma":
                var req = new CreateLanguageRequest(
                    Prompt("Código BCP-47 (ej: fr-FR)"),
                    Prompt("Nombre en inglés (ej: French)"),
                    Prompt("Nombre nativo (ej: Français)"));
                await ExecuteAsync(() => api.CreateLanguageAsync(req, ct), l => PrintLanguages([l]));
                break;

            case "Actualizar idioma":
                string upCode = Prompt("Código BCP-47 del idioma a actualizar");
                var upReq  = new UpdateLanguageRequest(
                    Prompt("Nuevo nombre en inglés"),
                    Prompt("Nuevo nombre nativo"));
                await ExecuteAsync(() => api.UpdateLanguageAsync(upCode, upReq, ct), l => PrintLanguages([l]));
                break;

            case "Activar / Desactivar":
                string actCode   = Prompt("Código BCP-47");
                bool isActive  = AnsiConsole.Confirm("¿Activar?");
                await ExecuteAsync(() => api.SetLanguageActiveAsync(actCode, isActive, ct), _ => Ok("Estado actualizado"));
                break;

            case "Establecer como predeterminado":
                string defCode = Prompt("Código BCP-47");
                await ExecuteAsync(() => api.SetDefaultLanguageAsync(defCode, ct), _ => Ok("Idioma predeterminado actualizado"));
                break;
        }
    }

    private static void PrintLanguages(IReadOnlyList<LanguageDto> languages)
    {
        Table table = new Table()
            .AddColumn("Código").AddColumn("Nombre").AddColumn("Nativo")
            .AddColumn("Activo").AddColumn("Predeterminado");

        foreach (LanguageDto l in languages)
            table.AddRow(
                l.Code, l.Name, l.NativeName,
                l.IsActive  ? "[green]✔[/]" : "[red]✘[/]",
                l.IsDefault ? "[yellow]★[/]" : "");

        AnsiConsole.Write(table);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // LOCALES
    // ══════════════════════════════════════════════════════════════════════════

    private async Task LocalesMenuAsync(CancellationToken ct)
    {
        string action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[blue]Locales[/] — Acción:")
                .AddChoices("Listar todos", "Obtener por idioma", "Crear / Actualizar (Upsert)", "← Volver"));

        switch (action)
        {
            case "Listar todos":
                await ExecuteAsync(() => api.GetAllLocalesAsync(ct), PrintLocales);
                break;

            case "Obtener por idioma":
                string code = Prompt("Código de idioma (ej: es-ES)");
                await ExecuteAsync(() => api.GetLocaleAsync(code, ct), l => PrintLocales([l]));
                break;

            case "Crear / Actualizar (Upsert)":
                string langCode = Prompt("Código de idioma");
                var req = new UpsertLocaleRequest(
                    Prompt("Formato de fecha largo   (ej: dd/MM/yyyy)"),
                    Prompt("Formato de fecha corto   (ej: d/M/yy)"),
                    Prompt("Formato de hora          (ej: HH:mm:ss)"),
                    Prompt("Formato de fecha y hora  (ej: dd/MM/yyyy HH:mm)"),
                    Prompt("Primer día de semana     (Monday|Sunday)"),
                    Prompt("Separador decimal        (ej: ,)"),
                    Prompt("Separador de miles       (ej: .)"),
                    int.Parse(Prompt("Decimales por defecto    (ej: 2)")),
                    int.Parse(Prompt("Decimales de moneda      (ej: 2)")));
                await ExecuteAsync(() => api.UpsertLocaleAsync(langCode, req, ct), l => PrintLocales([l]));
                break;
        }
    }

    private static void PrintLocales(IReadOnlyList<LocaleConfigurationDto> locales)
    {
        Table table = new Table()
            .AddColumn("Idioma").AddColumn("Fecha").AddColumn("Hora")
            .AddColumn("1er día").AddColumn("Dec.Sep").AddColumn("Miles Sep");

        foreach (LocaleConfigurationDto l in locales)
            table.AddRow(l.LanguageCode, l.DateFormat, l.TimeFormat,
                l.FirstDayOfWeek, l.DecimalSeparator, l.ThousandsSeparator);

        AnsiConsole.Write(table);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CURRENCIES
    // ══════════════════════════════════════════════════════════════════════════

    private async Task CurrenciesMenuAsync(CancellationToken ct)
    {
        string action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[blue]Currencies[/] — Acción:")
                .AddChoices("Listar por idioma", "Crear / Actualizar (Upsert)", "Eliminar", "← Volver"));

        switch (action)
        {
            case "Listar por idioma":
                string code = Prompt("Código de idioma (ej: es-ES)");
                await ExecuteAsync(() => api.GetCurrenciesAsync(code, ct), PrintCurrencies);
                break;

            case "Crear / Actualizar (Upsert)":
                string langCode  = Prompt("Código de idioma");
                string currCode  = Prompt("Código de moneda (ej: EUR)");
                string position  = AnsiConsole.Prompt(new SelectionPrompt<string>()
                    .Title("Posición del símbolo:").AddChoices("Before", "After"));
                var req = new UpsertCurrencyRequest(
                    Prompt("Nombre de la moneda      (ej: Euro)"),
                    Prompt("Símbolo                  (ej: €)"),
                    position,
                    AnsiConsole.Confirm("¿Espacio entre símbolo y cantidad?"),
                    Prompt("Separador decimal        (ej: ,)"),
                    Prompt("Separador de miles       (ej: .)"),
                    int.Parse(Prompt("Decimales (ej: 2)")),
                    Prompt("Patrón negativo          (ej: -{symbol}{amount})"));
                await ExecuteAsync(() => api.UpsertCurrencyAsync(langCode, currCode, req, ct), c => PrintCurrencies([c]));
                break;

            case "Eliminar":
                string delLang = Prompt("Código de idioma");
                string delCurr = Prompt("Código de moneda");
                if (AnsiConsole.Confirm($"¿Eliminar formato de [red]{delCurr}[/] para [red]{delLang}[/]?"))
                    await ExecuteAsync(() => api.DeleteCurrencyAsync(delLang, delCurr, ct), _ => Ok("Formato eliminado"));
                break;
        }
    }

    private static void PrintCurrencies(IReadOnlyList<CurrencyFormatDto> currencies)
    {
        Table table = new Table()
            .AddColumn("Idioma").AddColumn("Moneda").AddColumn("Nombre")
            .AddColumn("Símbolo").AddColumn("Posición").AddColumn("Ejemplo");

        foreach (var c in currencies)
            table.AddRow(c.LanguageCode, c.CurrencyCode, c.CurrencyName,
                c.Symbol, c.SymbolPosition, c.FormattedExample);

        AnsiConsole.Write(table);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // TRANSLATIONS
    // ══════════════════════════════════════════════════════════════════════════

    private async Task TranslationsMenuAsync(CancellationToken ct)
    {
        string action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[blue]Translations[/] — Acción:")
                .AddChoices(
                    "Obtener traducción (código + idioma)",
                    "Traducciones de un módulo",
                    "Listar paginado (admin)",
                    "Ver variantes de un código",
                    "Crear traducción",
                    "Actualizar traducción",
                    "Marcar como revisada",
                    "Eliminar traducción",
                    "Bulk Upsert (importación)",
                    "← Volver"));

        switch (action)
        {
            case "Obtener traducción (código + idioma)":
                (string code, string lang) = PromptCodeLang();
                await ExecuteAsync(() => api.GetTranslationAsync(code, lang, ct), PrintLookup);
                break;

            case "Traducciones de un módulo":
                string module  = Prompt("Módulo (ej: common)");
                string mlang   = Prompt("Código de idioma (ej: es-ES)");
                await ExecuteAsync(() => api.GetModuleTranslationsAsync(module, mlang, ct), PrintMap);
                break;

            case "Listar paginado (admin)":
                int page     = int.Parse(Prompt("Página (ej: 1)"));
                int pageSize = int.Parse(Prompt("Tamaño de página (ej: 25)"));
                string? fMod     = PromptOptional("Filtrar por módulo     (enter para omitir)");
                string? fLang    = PromptOptional("Filtrar por idioma     (enter para omitir)");
                string? fSort    = PromptOptional("Ordenar por            (enter para omitir)");
                await ExecuteAsync(
                    () => api.GetTranslationsPagedAsync(page, pageSize, fMod, fLang, fSort, ct),
                    PrintPaged);
                break;

            case "Ver variantes de un código":
                string vCode = Prompt("Código de traducción (ej: common.ok)");
                await ExecuteAsync(() => api.GetTranslationVariantsAsync(vCode, ct), PrintTranslations);
                break;

            case "Crear traducción":
                var cReq = new CreateTranslationRequest(
                    Prompt("Código          (ej: common.ok)"),
                    Prompt("Código de idioma (ej: es-ES)"),
                    Prompt("Texto"),
                    PromptOptional("Módulo  (enter para omitir)"),
                    PromptOptional("Contexto(enter para omitir)"));
                await ExecuteAsync(() => api.CreateTranslationAsync(cReq, ct), t => PrintTranslations([t]));
                break;

            case "Actualizar traducción":
                (string uCode, string uLang) = PromptCodeLang();
                var uReq = new UpdateTranslationRequest(
                    Prompt("Nuevo texto"),
                    PromptOptional("Nuevo contexto (enter para omitir)"));
                await ExecuteAsync(() => api.UpdateTranslationAsync(uCode, uLang, uReq, ct), t => PrintTranslations([t]));
                break;

            case "Marcar como revisada":
                (string rCode, string rLang) = PromptCodeLang();
                bool isReviewed = AnsiConsole.Confirm("¿Marcar como revisada?");
                await ExecuteAsync(
                    () => api.SetTranslationReviewedAsync(rCode, rLang, isReviewed, ct),
                    _ => Ok("Estado de revisión actualizado"));
                break;

            case "Eliminar traducción":
                (string dCode, string dLang) = PromptCodeLang();
                if (AnsiConsole.Confirm($"¿Eliminar [red]{dCode}/{dLang}[/]?"))
                    await ExecuteAsync(() => api.DeleteTranslationAsync(dCode, dLang, ct), _ => Ok("Traducción eliminada"));
                break;

            case "Bulk Upsert (importación)":
                await BulkUpsertFlowAsync(ct);
                break;
        }
    }

    private async Task BulkUpsertFlowAsync(CancellationToken ct)
    {
        AnsiConsole.MarkupLine("[grey]Introduce las entradas. Escribe [bold]FIN[/] cuando termines.[/]");

        var entries = new List<BulkTranslationEntry>();
        while (true)
        {
            string code = Prompt("Código (o FIN para terminar)");
            if (code.Equals("FIN", StringComparison.OrdinalIgnoreCase)) break;

            entries.Add(new BulkTranslationEntry(
                code,
                Prompt("Código de idioma"),
                Prompt("Texto"),
                PromptOptional("Módulo (enter para omitir)")));

            AnsiConsole.MarkupLine($"[grey]→ {entries.Count} entradas en cola[/]");
        }

        if (entries.Count == 0) { AnsiConsole.MarkupLine("[yellow]Sin entradas.[/]"); return; }

        await ExecuteAsync(
            () => api.BulkUpsertTranslationsAsync(new BulkUpsertRequest(entries), ct),
            r => AnsiConsole.MarkupLine(
                $"[green]✔[/] Insertadas: [green]{r.Inserted}[/]  Actualizadas: [yellow]{r.Updated}[/]  Omitidas: [grey]{r.Skipped}[/]"));
    }

    private static void PrintLookup(TranslationLookupDto t)
    {
        Panel panel = new Panel(
            $"[bold]{t.Text}[/]\n" +
            $"[grey]Idioma resuelto: {t.ResolvedLanguageCode}  " +
            $"Fallback: {(t.IsFallback ? "[yellow]Sí[/]" : "[green]No[/]")}[/]")
            .Header($"[blue]{t.Code}[/] / {t.LanguageCode}");
        AnsiConsole.Write(panel);
    }

    private static void PrintMap(TranslationMapDto map)
    {
        AnsiConsole.MarkupLine($"[blue]Módulo:[/] {map.Module}  [blue]Idioma:[/] {map.LanguageCode}  [grey]({map.Translations.Count} claves)[/]");
        Table table = new Table().AddColumn("Clave").AddColumn("Texto");
        foreach (KeyValuePair<string, string> kv in map.Translations.Take(50))
            table.AddRow(kv.Key, kv.Value.EscapeMarkup());
        AnsiConsole.Write(table);
        if (map.Translations.Count > 50)
            AnsiConsole.MarkupLine($"[grey]... y {map.Translations.Count - 50} más[/]");
    }

    private static void PrintPaged(PagedResult<TranslationDto> paged)
    {
        AnsiConsole.MarkupLine($"[grey]Página {paged.Page}/{paged.TotalPages}  —  Total: {paged.TotalCount} traducciones[/]");
        PrintTranslations(paged.Items);
    }

    private static void PrintTranslations(IReadOnlyList<TranslationDto> translations)
    {
        Table table = new Table()
            .AddColumn("Código").AddColumn("Idioma").AddColumn("Texto").AddColumn("Revisada");

        foreach (TranslationDto t in translations)
            table.AddRow(
                t.Code, t.LanguageCode,
                t.Text.Length > 60 ? t.Text[..60].EscapeMarkup() + "…" : t.Text.EscapeMarkup(),
                t.IsReviewed ? "[green]✔[/]" : "[grey]—[/]");

        AnsiConsole.Write(table);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CONFIGURACIÓN
    // ══════════════════════════════════════════════════════════════════════════

    private Task ConfigMenuAsync()
    {
        AnsiConsole.Write(new Rule("[yellow]Configuración actual[/]"));

        Table table = new Table().AddColumn("Clave").AddColumn("Valor");
        table.AddRow("Api:BaseUrl", apiSettings.BaseUrl);
        table.AddRow("Api:BearerToken", string.IsNullOrWhiteSpace(apiSettings.BearerToken) ? "[red]⚠ Vacío[/]" : "[green]****[/]");
        table.AddRow("Atlas:Database:Provider", dbSettings.Provider.ToString());
        table.AddRow("Atlas:Database:ConnectionString", MaskConnectionString(dbSettings.ConnectionString));
        table.AddRow("Atlas:Database:CommandTimeoutSeconds", dbSettings.CommandTimeoutSeconds.ToString());
        table.AddRow("Atlas:Database:Retry:MaxRetryCount", dbSettings.Retry.MaxRetryCount.ToString());
        table.AddRow("Atlas:Database:Pool:MaxSize", dbSettings.Pool.MaxSize.ToString());
        AnsiConsole.Write(table);

        AnsiConsole.MarkupLine("\n[grey]Edita [bold]appsettings.json[/] para cambiar la configuración y reinicia la aplicación.[/]");
        AnsiConsole.MarkupLine(
            $"\n[bold]Providers admitidos en appsettings.json:[/]\n" +
            $"  [cyan]PostgreSql[/]  — Npgsql\n" +
            $"  [red]SqlServer[/]   — Microsoft.EntityFrameworkCore.SqlServer\n" +
            $"  [orange1]MySql[/]       — Pomelo.EntityFrameworkCore.MySql");

        Pause();
        return Task.CompletedTask;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Utilities
    // ══════════════════════════════════════════════════════════════════════════

    private static async Task ExecuteAsync<T>(Func<Task<ApiResult<T>>> call, Action<T> onSuccess)
    {
        T? result = default;
        ApiError? error = null;

        await AnsiConsole.Status().StartAsync("Conectando...", async _ =>
        {
            ApiResult<T> r = await call();
            if (r.IsSuccess) result = r.Value;
            else             error  = r.Error;
        });

        if (error is not null)
        {
            AnsiConsole.MarkupLine($"[red]✘ Error {error.Status}:[/] {error.Title}");
            if (!string.IsNullOrWhiteSpace(error.Detail))
                AnsiConsole.MarkupLine($"  [grey]{error.Detail.EscapeMarkup()}[/]");
        }
        else if (result is not null)
        {
            onSuccess(result);
        }

        Pause();
    }

    private static string  Prompt(string label)         => AnsiConsole.Ask<string>($"[grey]{label}:[/]");
    private static string? PromptOptional(string label)
    {
        string value = AnsiConsole.Prompt(
            new TextPrompt<string>($"[grey]{label}:[/]").AllowEmpty());
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
    private static (string code, string lang) PromptCodeLang() => (Prompt("Código de traducción"), Prompt("Código de idioma"));
    private static void Ok(string msg)   => AnsiConsole.MarkupLine($"[green]✔ {msg}[/]");
    private static void Pause()
    {
        AnsiConsole.Markup("\n[grey]Pulsa Enter para continuar...[/]");
        Console.ReadLine();
    }

    private static string MaskConnectionString(string cs)
    {
        if (string.IsNullOrWhiteSpace(cs)) return "[red]⚠ Vacío[/]";
        // Hide passwords
        var masked = System.Text.RegularExpressions.Regex.Replace(
            cs, @"(?i)(password|pwd)=[^;]+", "$1=****");
        return masked.Length > 80 ? masked[..80] + "…" : masked;
    }
}
