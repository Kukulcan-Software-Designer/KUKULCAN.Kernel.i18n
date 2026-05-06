namespace ATLAS.Kernel.i18n.Domain.DTOs;

/// <summary>
/// Full module string table returned by the bulk-module endpoint.
/// Key: translation code (e.g. <c>"CRM0001"</c>). Value: translated text.
/// </summary>
/// <param name="LanguageCode"></param>
/// <param name="Module"></param>
/// <param name="Translations"></param>
public record TranslationMapDto(
    string LanguageCode,
    string Module,
    IReadOnlyDictionary<string, string> Translations);
