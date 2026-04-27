using ATLAS.i18n.Domain.Entities;
using ATLAS.i18n.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ATLAS.i18n.Infrastructure.Persistence.Seeds;

/// <summary>
/// Seeds the mandatory base data required for ATLAS.i18n to operate:
///   - English (EN) — default language
///   - Spanish (ES)
///   - Catalan (CA)
///   - French (FR)
///   - German (DE)
///   - Locale configurations for EN and ES (the two guaranteed languages)
///   - Common currency formats: USD and EUR in both EN and ES
///   - Core system translations (CORE module) in EN and ES
/// </summary>
public static class I18nSeedData
{
    public static async Task SeedAsync(I18nDbContext context)
    {
        await SeedLanguagesAsync(context);
        await SeedLocaleConfigurationsAsync(context);
        await SeedCurrencyFormatsAsync(context);
        await SeedCoreTranslationsAsync(context);

        await context.SaveChangesAsync();
    }

    // ─── Languages ────────────────────────────────────────────────────────────

    private static async Task SeedLanguagesAsync(I18nDbContext context)
    {
        var languages = new[]
        {
            Language.Create("EN", "English",    "English",    "en-US", isDefault: true),
            Language.Create("ES", "Spanish",    "Español",    "es-ES"),
            Language.Create("CA", "Catalan",    "Català",     "ca-ES"),
            Language.Create("FR", "French",     "Français",   "fr-FR"),
            Language.Create("DE", "German",     "Deutsch",    "de-DE"),
            Language.Create("PT", "Portuguese", "Português",  "pt-PT"),
            Language.Create("IT", "Italian",    "Italiano",   "it-IT"),
        };

        foreach (var lang in languages)
        {
            var code = lang.Id.Value;
            if (!await context.Languages.AnyAsync(l => EF.Property<string>(l, "Code") == code))
                context.Languages.Add(lang);
        }
    }

    // ─── Locale Configurations ────────────────────────────────────────────────

    private static async Task SeedLocaleConfigurationsAsync(I18nDbContext context)
    {
        var configs = new[]
        {
            // English (US): MM/dd/yyyy, 12h clock, Sunday first, period decimal
            LocaleConfiguration.Create(
                "EN",
                dateFormat:            "MM/dd/yyyy",
                shortDateFormat:       "M/d/yy",
                timeFormat:            "h:mm tt",
                dateTimeFormat:        "MM/dd/yyyy h:mm tt",
                firstDayOfWeek:        FirstDayOfWeek.Sunday,
                decimalSeparator:      '.',
                thousandsSeparator:    ',',
                decimalPlaces:         2,
                currencyDecimalPlaces: 2),

            // Spanish: dd/MM/yyyy, 24h clock, Monday first, comma decimal
            LocaleConfiguration.Create(
                "ES",
                dateFormat:            "dd/MM/yyyy",
                shortDateFormat:       "d/M/yy",
                timeFormat:            "HH:mm",
                dateTimeFormat:        "dd/MM/yyyy HH:mm",
                firstDayOfWeek:        FirstDayOfWeek.Monday,
                decimalSeparator:      ',',
                thousandsSeparator:    '.',
                decimalPlaces:         2,
                currencyDecimalPlaces: 2),

            // Catalan: dd/MM/yyyy, 24h, Monday first, comma decimal (same as ES)
            LocaleConfiguration.Create(
                "CA",
                dateFormat:            "dd/MM/yyyy",
                shortDateFormat:       "d/M/yy",
                timeFormat:            "HH:mm",
                dateTimeFormat:        "dd/MM/yyyy HH:mm",
                firstDayOfWeek:        FirstDayOfWeek.Monday,
                decimalSeparator:      ',',
                thousandsSeparator:    '.',
                decimalPlaces:         2,
                currencyDecimalPlaces: 2),

            // French: dd/MM/yyyy, 24h, Monday first, comma decimal, space thousands
            LocaleConfiguration.Create(
                "FR",
                dateFormat:            "dd/MM/yyyy",
                shortDateFormat:       "d/M/yy",
                timeFormat:            "HH:mm",
                dateTimeFormat:        "dd/MM/yyyy HH:mm",
                firstDayOfWeek:        FirstDayOfWeek.Monday,
                decimalSeparator:      ',',
                thousandsSeparator:    ' ',
                decimalPlaces:         2,
                currencyDecimalPlaces: 2),

            // German: dd.MM.yyyy, 24h, Monday first, comma decimal, period thousands
            LocaleConfiguration.Create(
                "DE",
                dateFormat:            "dd.MM.yyyy",
                shortDateFormat:       "d.M.yy",
                timeFormat:            "HH:mm",
                dateTimeFormat:        "dd.MM.yyyy HH:mm",
                firstDayOfWeek:        FirstDayOfWeek.Monday,
                decimalSeparator:      ',',
                thousandsSeparator:    '.',
                decimalPlaces:         2,
                currencyDecimalPlaces: 2),
        };

        foreach (var cfg in configs)
        {
            var langCode = cfg.LanguageCode.Value;
            if (!await context.LocaleConfigurations.AnyAsync(
                    lc => EF.Property<string>(lc, "LanguageCode") == langCode))
                context.LocaleConfigurations.Add(cfg);
        }
    }

    // ─── Currency Formats ─────────────────────────────────────────────────────

    private static async Task SeedCurrencyFormatsAsync(I18nDbContext context)
    {
        var formats = new[]
        {
            // USD in English: $1,234.56 / ($1,234.56) negative
            CurrencyFormat.Create("EN", "USD", "US Dollar",   "$",  CurrencySymbolPosition.Before, false, '.', ',', 2, "({symbol}{amount})"),
            // EUR in English: €1,234.56
            CurrencyFormat.Create("EN", "EUR", "Euro",         "€",  CurrencySymbolPosition.Before, false, '.', ',', 2, "-{symbol}{amount}"),
            // GBP in English: £1,234.56
            CurrencyFormat.Create("EN", "GBP", "British Pound","£",  CurrencySymbolPosition.Before, false, '.', ',', 2, "-{symbol}{amount}"),
            // JPY in English: ¥1,234 (0 decimals)
            CurrencyFormat.Create("EN", "JPY", "Japanese Yen", "¥",  CurrencySymbolPosition.Before, false, '.', ',', 0, "-{symbol}{amount}"),

            // EUR in Spanish: 1.234,56 €
            CurrencyFormat.Create("ES", "EUR", "Euro",         "€",  CurrencySymbolPosition.After,  true,  ',', '.', 2, "-{amount} {symbol}"),
            // USD in Spanish: 1.234,56 $
            CurrencyFormat.Create("ES", "USD", "Dólar estadounidense", "$", CurrencySymbolPosition.After, true,  ',', '.', 2, "-{amount} {symbol}"),
            // GBP in Spanish: 1.234,56 £
            CurrencyFormat.Create("ES", "GBP", "Libra esterlina",      "£", CurrencySymbolPosition.After, true,  ',', '.', 2, "-{amount} {symbol}"),

            // EUR in Catalan: 1.234,56 €
            CurrencyFormat.Create("CA", "EUR", "Euro",         "€",  CurrencySymbolPosition.After,  true,  ',', '.', 2, "-{amount} {symbol}"),

            // EUR in French: 1 234,56 €
            CurrencyFormat.Create("FR", "EUR", "Euro",         "€",  CurrencySymbolPosition.After,  true,  ',', ' ', 2, "-{amount} {symbol}"),

            // EUR in German: 1.234,56 €
            CurrencyFormat.Create("DE", "EUR", "Euro",         "€",  CurrencySymbolPosition.After,  true,  ',', '.', 2, "-{amount} {symbol}"),
        };

        foreach (var fmt in formats)
        {
            var lang = fmt.LanguageCode.Value;
            var curr = fmt.CurrencyCode;
            if (!await context.CurrencyFormats.AnyAsync(
                    cf => EF.Property<string>(cf, "LanguageCode") == lang
                       && cf.CurrencyCode == curr))
                context.CurrencyFormats.Add(fmt);
        }
    }

    // ─── Core System Translations ─────────────────────────────────────────────

    private static async Task SeedCoreTranslationsAsync(I18nDbContext context)
    {
        // Format: (code, module, seq, EN text, ES text, context)
        var entries = new[]
        {
            // Generic errors
            ("CORE", 1,  "Not found.",                           "No encontrado.",
             "Generic 404 message"),
            ("CORE", 2,  "Validation error.",                    "Error de validación.",
             "Generic validation error"),
            ("CORE", 3,  "Unauthorized.",                        "No autorizado.",
             "HTTP 401 message"),
            ("CORE", 4,  "Forbidden.",                           "Acceso denegado.",
             "HTTP 403 message"),
            ("CORE", 5,  "Internal server error.",               "Error interno del servidor.",
             "HTTP 500 message"),
            ("CORE", 6,  "Bad request.",                         "Solicitud incorrecta.",
             "HTTP 400 message"),
            ("CORE", 7,  "Service unavailable.",                 "Servicio no disponible.",
             "HTTP 503 message"),
            ("CORE", 8,  "Operation completed successfully.",    "Operación completada con éxito.",
             "Generic success message"),
            ("CORE", 9,  "The field '{0}' is required.",         "El campo '{0}' es obligatorio.",
             "Field required validation. {0} = field name"),
            ("CORE", 10, "The field '{0}' is invalid.",          "El campo '{0}' no es válido.",
             "Field invalid validation. {0} = field name"),
            // Pagination
            ("CORE", 20, "Page {0} of {1}.",                     "Página {0} de {1}.",
             "Pagination info. {0}=current, {1}=total"),
            ("CORE", 21, "No results found.",                    "No se encontraron resultados.",
             "Empty list message"),
            // i18n module self-descriptions
            ("CORE", 50, "Translation not found.",               "Traducción no encontrada.",
             "When a translation code does not exist"),
            ("CORE", 51, "Language not supported.",              "Idioma no soportado.",
             "When a requested language is inactive/missing"),
        };

        foreach (var (module, seq, enText, esText, ctx) in entries)
        {
            var code   = $"{module}{seq:D4}";
            var enLang = "EN";
            var esLang = "ES";

            if (!await context.Translations.AnyAsync(
                    t => EF.Property<string>(t, "Code") == code
                      && EF.Property<string>(t, "LanguageCode") == enLang))
            {
                var t = Translation.Create(code, enLang, enText, ctx);
                t.MarkAsReviewed();
                context.Translations.Add(t);
            }

            if (!await context.Translations.AnyAsync(
                    t => EF.Property<string>(t, "Code") == code
                      && EF.Property<string>(t, "LanguageCode") == esLang))
            {
                var t = Translation.Create(code, esLang, esText, ctx);
                t.MarkAsReviewed();
                context.Translations.Add(t);
            }
        }
    }
}
