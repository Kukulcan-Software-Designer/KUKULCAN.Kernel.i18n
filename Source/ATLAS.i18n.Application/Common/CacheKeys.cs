namespace ATLAS.i18n.Application.Common;

/// <summary>
/// Centralised cache key definitions for ATLAS.i18n.
/// All keys are prefixed with <c>"i18n:"</c> to avoid collisions with other modules.
///
/// <para>
/// Keys must include the tenant identifier only when the cached data is tenant-specific.
/// Language, translation, locale, and currency data is <b>global</b> in ATLAS.i18n,
/// so tenant IDs are intentionally absent from these keys.
/// </para>
/// </summary>
public static class I18nCacheKeys
{
    // ── Language ──────────────────────────────────────────────────────────────

    /// <summary>All languages (active + inactive).</summary>
    public const string LanguagesAll    = "i18n:languages:all";

    /// <summary>Active languages only.</summary>
    public const string LanguagesActive = "i18n:languages:active";

    /// <summary>The single default language.</summary>
    public const string LanguageDefault = "i18n:languages:default";

    /// <summary>Single language by BCP-47 code.</summary>
    public static string Language(string bcp47Code)
        => $"i18n:language:{bcp47Code.ToLowerInvariant()}";

    // ── Translation ───────────────────────────────────────────────────────────

    /// <summary>Single translation lookup result (code + language).</summary>
    public static string Translation(string code, string lang)
        => $"i18n:t:{code.ToUpperInvariant()}:{lang.ToLowerInvariant()}";

    /// <summary>Full module string table for a language.</summary>
    public static string ModuleTranslations(string module, string lang)
        => $"i18n:module:{module.ToUpperInvariant()}:{lang.ToLowerInvariant()}";

    // ── Locale ────────────────────────────────────────────────────────────────

    /// <summary>Locale configuration for a language.</summary>
    public static string LocaleConfig(string lang)
        => $"i18n:locale:{lang.ToLowerInvariant()}";

    // ── Currency ──────────────────────────────────────────────────────────────

    /// <summary>All currency formats for a language.</summary>
    public static string CurrencyFormats(string lang)
        => $"i18n:currencies:{lang.ToLowerInvariant()}";

    /// <summary>Single currency format (language + ISO 4217 code).</summary>
    public static string CurrencyFormat(string lang, string currencyCode)
        => $"i18n:currency:{lang.ToLowerInvariant()}:{currencyCode.ToUpperInvariant()}";
}
