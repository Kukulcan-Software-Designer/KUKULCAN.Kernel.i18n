using KUKULCAN.Kernel.Infrastructure.Primitives;
using KUKULCAN.Kernel.i18n.Domain.ValueObjects.Enums;

namespace KUKULCAN.Kernel.i18n.Infrastructure.Persistence.Seeds;

/// <summary>
/// Seeds the mandatory base data required for KUKULCAN.Kernel.i18n:
/// <list type="bullet">
///   <item>Languages: EN (default), ES, CA, FR, DE, PT, IT</item>
///   <item>Locale configurations for each language</item>
///   <item>Common currency formats: USD, EUR, GBP (EN and ES); EUR, USD (CA, FR, DE)</item>
///   <item>Core system translations (CORE module) in EN and ES</item>
/// </list>
/// <para>
/// All factory methods use the <c>Result</c> pattern from SharedKernel.
/// Seeding is idempotent — safe to call on every startup.
/// </para>
/// </summary>
public static class I18NSeedData
{
    /// <summary>
    /// Executes SeedAsync.
    /// </summary>
    /// <param name="ctx">The ctx parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The operation result.</returns>
    public static async Task SeedAsync(I18NDbContext ctx, CancellationToken ct = default)
    {
        await SeedLanguagesAsync(ctx, ct);
        await SeedLocaleConfigurationsAsync(ctx, ct);
        await SeedCurrencyFormatsAsync(ctx, ct);
        await SeedCoreTranslationsAsync(ctx, ct);
        await ctx.SaveChangesAsync(ct);
    }

    // ─── Languages ────────────────────────────────────────────────────────────

    private static async Task SeedLanguagesAsync(I18NDbContext ctx, CancellationToken ct)
    {
        var languages = new[]
        {
            ("en-US", "English",    "English",    true),
            ("es-ES", "Spanish",    "Español",    false),
            ("ca-ES", "Catalan",    "Català",     false),
            ("fr-FR", "French",     "Français",   false),
            ("de-DE", "German",     "Deutsch",    false),
            ("pt-PT", "Portuguese", "Português",  false),
            ("it-IT", "Italian",    "Italiano",   false),
        };

        foreach (var (code, name, nativeName, isDefault) in languages)
        {
            if (await ctx.Languages.AnyAsync(l => l.Code == code, ct)) continue;

            var result = Language.Create(
                SequentialGuid.NewSequentialGuidAtEnd(), code, name, nativeName, isDefault);

            if (result.IsSuccess)
                await ctx.Languages.AddAsync(result.Value, ct);
        }
    }

    // ─── Locale Configurations ────────────────────────────────────────────────

    private static async Task SeedLocaleConfigurationsAsync(I18NDbContext ctx, CancellationToken ct)
    {
        // (languageCode, dateFormat, shortDateFormat, timeFormat, dateTimeFormat,
        //  firstDayOfWeek, decimalSep, thousandsSep, decPlaces, currDecPlaces)
        var configs = new[]
        {
            ("en-US", "MM/dd/yyyy", "M/d/yy",   "h:mm tt",  "MM/dd/yyyy h:mm tt",  FirstDayOfWeek.Sunday,  '.', ',', 2, 2),
            ("es-ES", "dd/MM/yyyy", "d/M/yy",   "HH:mm",    "dd/MM/yyyy HH:mm",    FirstDayOfWeek.Monday,  ',', '.', 2, 2),
            ("ca-ES", "dd/MM/yyyy", "d/M/yy",   "HH:mm",    "dd/MM/yyyy HH:mm",    FirstDayOfWeek.Monday,  ',', '.', 2, 2),
            ("fr-FR", "dd/MM/yyyy", "d/M/yy",   "HH:mm",    "dd/MM/yyyy HH:mm",    FirstDayOfWeek.Monday,  ',', ' ', 2, 2),
            ("de-DE", "dd.MM.yyyy", "d.M.yy",   "HH:mm",    "dd.MM.yyyy HH:mm",    FirstDayOfWeek.Monday,  ',', '.', 2, 2),
            ("pt-PT", "dd/MM/yyyy", "d/M/yy",   "HH:mm",    "dd/MM/yyyy HH:mm",    FirstDayOfWeek.Monday,  ',', '.', 2, 2),
            ("it-IT", "dd/MM/yyyy", "d/M/yy",   "HH:mm",    "dd/MM/yyyy HH:mm",    FirstDayOfWeek.Monday,  ',', '.', 2, 2),
        };

        foreach (var (lang, date, shortDate, time, dateTime, dow, dec, thou, dp, cdp) in configs)
        {
            if (await ctx.LocaleConfigurations.AnyAsync(
                lc => lc.LanguageCode == LanguageCode.Create(lang).Value, ct))
                continue;

            var result = LocaleConfiguration.Create(
                SequentialGuid.NewSequentialGuidAtEnd(),
                lang, date, shortDate, time, dateTime, dow, dec, thou, dp, cdp);

            if (result.IsSuccess)
                await ctx.LocaleConfigurations.AddAsync(result.Value, ct);
        }
    }

    // ─── Currency Formats ─────────────────────────────────────────────────────

    private static async Task SeedCurrencyFormatsAsync(I18NDbContext ctx, CancellationToken ct)
    {
        // (lang, iso4217, name, symbol, position, space, decSep, thousSep, decPlaces, negativePattern)
        var formats = new[]
        {
            // English
            ("en-US", "USD", "US Dollar", "$", CurrencySymbolPosition.Before, false, '.', ',', 2, "({symbol}{amount})"),
            ("en-US", "EUR", "Euro", "€", CurrencySymbolPosition.Before, false, '.', ',', 2, "-{symbol}{amount}"),
            ("en-US", "GBP", "British Pound", "£", CurrencySymbolPosition.Before, false, '.', ',', 2, "-{symbol}{amount}"),
            ("en-US", "JPY", "Japanese Yen", "¥", CurrencySymbolPosition.Before, false, '.', ',', 0, "-{symbol}{amount}"),
            // Spanish
            ("es-ES", "EUR", "Euro", "€", CurrencySymbolPosition.After, true, ',', '.', 2, "-{amount} {symbol}"),
            ("es-ES", "USD", "Dólar estadounidense", "$", CurrencySymbolPosition.After, true, ',', '.', 2, "-{amount} {symbol}"),
            ("es-ES", "GBP", "Libra esterlina", "£", CurrencySymbolPosition.After, true, ',', '.', 2, "-{amount} {symbol}"),
            // Catalan
            ("ca-ES", "EUR", "Euro", "€", CurrencySymbolPosition.After, true, ',', '.', 2, "-{amount} {symbol}"),
            ("ca-ES", "USD", "Dòlar estatunidenc", "$", CurrencySymbolPosition.After, true, ',', '.', 2, "-{amount} {symbol}"),
            // French
            ("fr-FR", "EUR", "Euro", "€", CurrencySymbolPosition.After, true, ',', ' ', 2, "-{amount} {symbol}"),
            ("fr-FR", "USD", "Dollar américain", "$", CurrencySymbolPosition.After, true, ',', ' ', 2, "-{amount} {symbol}"),
            // German
            ("de-DE", "EUR", "Euro", "€", CurrencySymbolPosition.After, true, ',', '.', 2, "-{amount} {symbol}"),
            ("de-DE", "USD", "US-Dollar", "$", CurrencySymbolPosition.After, true, ',', '.', 2, "-{amount} {symbol}"),
        };

        foreach (var (lang, iso, name, sym, pos, space, dec, thou, dp, neg) in formats)
        {
            var langCode = LanguageCode.Create(lang).Value;
            if (await ctx.CurrencyFormats.AnyAsync(
                cf => cf.LanguageCode == langCode && cf.CurrencyCode == iso, ct))
                continue;

            var result = CurrencyFormat.Create(
                SequentialGuid.NewSequentialGuidAtEnd(),
                lang, iso, name, sym, pos, space, dec, thou, dp, neg);

            if (result.IsSuccess)
                await ctx.CurrencyFormats.AddAsync(result.Value, ct);
        }
    }

    // ─── Core Translations ────────────────────────────────────────────────────

    private static async Task SeedCoreTranslationsAsync(I18NDbContext ctx, CancellationToken ct)
    {
        // (module, seq, en text, es text, context)
        var entries = new[]
        {
            ("CORE", 1,  "Not found.",                          "No encontrado.",                "HTTP 404"),
            ("CORE", 2,  "Validation error.",                   "Error de validación.",          "HTTP 422"),
            ("CORE", 3,  "Unauthorized.",                       "No autorizado.",                "HTTP 401"),
            ("CORE", 4,  "Forbidden.",                          "Acceso denegado.",              "HTTP 403"),
            ("CORE", 5,  "Internal server error.",              "Error interno del servidor.",   "HTTP 500"),
            ("CORE", 6,  "Bad request.",                        "Solicitud incorrecta.",         "HTTP 400"),
            ("CORE", 7,  "Service unavailable.",                "Servicio no disponible.",       "HTTP 503"),
            ("CORE", 8,  "Operation completed successfully.",   "Operación completada con éxito.", "Generic success"),
            ("CORE", 9,  "The field '{0}' is required.",        "El campo '{0}' es obligatorio.", "{0}=field name"),
            ("CORE", 10, "The field '{0}' is invalid.",         "El campo '{0}' no es válido.",  "{0}=field name"),
            ("CORE", 20, "Page {0} of {1}.",                    "Página {0} de {1}.",            "{0}=current, {1}=total"),
            ("CORE", 21, "No results found.",                   "No se encontraron resultados.", "Empty list"),
            ("CORE", 50, "Translation not found.",              "Traducción no encontrada.",     "i18n self"),
            ("CORE", 51, "Language not supported.",             "Idioma no soportado.",          "i18n self"),
        };

        foreach (var (module, seq, en, es, ctx_) in entries)
        {
            var code = $"{module}{seq:D4}";

            foreach (var (langCode, text) in new[] { ("en-US", en), ("es-ES", es) })
            {
                var langResult = LanguageCode.Create(langCode);
                if (await ctx.Translations.AnyAsync(
                    t => t.Code == TranslationCode.From(code).Value &&
                         t.LanguageCode == langResult.Value, ct))
                    continue;

                var result = Translation.Create(
                    SequentialGuid.NewSequentialGuidAtEnd(),
                    code, langCode, text, ctx_);

                if (result.IsSuccess)
                {
                    result.Value.MarkAsReviewed();
                    await ctx.Translations.AddAsync(result.Value, ct);
                }
            }
        }
    }
}
