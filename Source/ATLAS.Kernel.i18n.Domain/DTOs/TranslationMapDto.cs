namespace ATLAS.Kernel.i18n.Domain.DTOs;

/// <summary>
/// Full module string table returned by the bulk-module endpoint.
/// Key: translation code (e.g. <c>"CRM0001"</c>). Value: translated text.
/// </summary>
/// <param name="LanguageCode">The LanguageCode parameter.</param>
/// <param name="Module">The Module parameter.</param>
/// <param name="Translations">The Translations parameter.</param>
public record TranslationMapDto(string LanguageCode, string Module, IReadOnlyDictionary<string, string> Translations);
