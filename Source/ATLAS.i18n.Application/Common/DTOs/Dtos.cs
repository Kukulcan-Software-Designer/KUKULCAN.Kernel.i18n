namespace ATLAS.i18n.Application.Common.DTOs;

// ─── Language ─────────────────────────────────────────────────────────────────

public record LanguageDto(
    string Code,
    string Name,
    string NativeName,
    string CultureTag,
    bool IsDefault,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

// ─── Translation ──────────────────────────────────────────────────────────────

public record TranslationDto(
    Guid Id,
    string Code,
    string Module,
    string LanguageCode,
    string Text,
    string? Context,
    int? MaxLength,
    bool IsReviewed,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>Lightweight result used in lookup / resolution scenarios.</summary>
public record TranslationLookupDto(
    string Code,
    string LanguageCode,
    string Text,
    bool IsFallback);

/// <summary>Used for bulk translation responses (module / language dictionaries).</summary>
public record TranslationMapDto(
    string LanguageCode,
    string Module,
    /// <summary>Key: translation code (e.g. "CRM0001"), Value: translated text.</summary>
    IReadOnlyDictionary<string, string> Translations);

// ─── Locale Configuration ────────────────────────────────────────────────────

public record LocaleConfigurationDto(
    string LanguageCode,
    string DateFormat,
    string ShortDateFormat,
    string TimeFormat,
    string DateTimeFormat,
    string FirstDayOfWeek,
    string DecimalSeparator,
    string ThousandsSeparator,
    int DecimalPlaces,
    int CurrencyDecimalPlaces);

// ─── Currency Format ──────────────────────────────────────────────────────────

public record CurrencyFormatDto(
    Guid Id,
    string LanguageCode,
    string CurrencyCode,
    string CurrencyName,
    string Symbol,
    string SymbolPosition,
    bool SpaceBetweenSymbolAndAmount,
    string DecimalSeparator,
    string ThousandsSeparator,
    int DecimalPlaces,
    string NegativePattern,
    /// <summary>Example: "1.234,56 €" — formatted sample using 1234.56.</summary>
    string FormattedExample);

// ─── Paging ───────────────────────────────────────────────────────────────────

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
}
