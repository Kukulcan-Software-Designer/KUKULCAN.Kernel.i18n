namespace ATLAS.Kernel.i18n.Domain.DTOs;

/// <summary>
/// Lightweight result for lookup / hot-path scenarios.
/// </summary>
/// <param name="Code"></param>
/// <param name="LanguageCode"></param>
/// <param name="Text"></param>
/// <param name="IsFallback"><c>true</c> when the actual language returned differs from the requested one.</param>
/// <param name="ActualLanguageCode">The language actually used (may differ from requested when fallback applied).</param>
public record TranslationLookupDto(
    string Code,
    string LanguageCode,
    string Text,
    bool IsFallback,
    string ActualLanguageCode);
