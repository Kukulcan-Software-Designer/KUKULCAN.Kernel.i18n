namespace ATLAS.i18n.Application.Common.DTOs;

// ── Language ──────────────────────────────────────────────────────────────────

public record LanguageDto(
    Guid            Id,
    string          Code,
    string          Name,
    string          NativeName,
    bool            IsDefault,
    bool            IsActive,
    DateTimeOffset  CreatedAt,
    string          CreatedBy,
    DateTimeOffset? UpdatedAt,
    string?         UpdatedBy);

// ── Translation ───────────────────────────────────────────────────────────────

public record TranslationDto(
    Guid            Id,
    string          Code,
    string          Module,
    string          LanguageCode,
    string          Text,
    string?         Context,
    int?            MaxLength,
    bool            IsReviewed,
    DateTimeOffset  CreatedAt,
    string          CreatedBy,
    DateTimeOffset? UpdatedAt,
    string?         UpdatedBy);

/// <summary>Lightweight result for lookup / hot-path scenarios.</summary>
public record TranslationLookupDto(
    string Code,
    string LanguageCode,
    string Text,
    /// <summary><c>true</c> when the actual language returned differs from the requested one.</summary>
    bool   IsFallback,
    /// <summary>The language actually used (may differ from requested when fallback applied).</summary>
    string ActualLanguageCode);

/// <summary>
/// Full module string table returned by the bulk-module endpoint.
/// Key: translation code (e.g. <c>"CRM0001"</c>). Value: translated text.
/// </summary>
public record TranslationMapDto(
    string                              LanguageCode,
    string                              Module,
    IReadOnlyDictionary<string, string> Translations);

// ── Locale Configuration ──────────────────────────────────────────────────────

public record LocaleConfigurationDto(
    string          LanguageCode,
    string          DateFormat,
    string          ShortDateFormat,
    string          TimeFormat,
    string          DateTimeFormat,
    string          FirstDayOfWeek,
    string          DecimalSeparator,
    string          ThousandsSeparator,
    int             DecimalPlaces,
    int             CurrencyDecimalPlaces,
    DateTimeOffset  CreatedAt,
    DateTimeOffset? UpdatedAt);

// ── Currency Format ───────────────────────────────────────────────────────────

public record CurrencyFormatDto(
    Guid            Id,
    string          LanguageCode,
    string          CurrencyCode,
    string          CurrencyName,
    string          Symbol,
    string          SymbolPosition,
    bool            SpaceBetweenSymbolAndAmount,
    string          DecimalSeparator,
    string          ThousandsSeparator,
    int             DecimalPlaces,
    string          NegativePattern,
    /// <summary>Pre-formatted example: <c>"1.234,56 €"</c> (using 1 234.56).</summary>
    string          FormattedExample,
    DateTimeOffset  CreatedAt,
    DateTimeOffset? UpdatedAt);

// ── Bulk import ───────────────────────────────────────────────────────────────

public record BulkUpsertResultDto(
    int                    Created,
    int                    Updated,
    IReadOnlyList<string>  Errors);
