namespace ATLAS.Kernel.i18n.Domain.Interfaces.Services;

/// <summary>
/// Resolves the text for a translation code in the requested language,
/// walking the BCP-47 fallback chain until a match is found.
///
/// <para>
/// Uses <see cref="LanguageCode.FallbackChain"/> from <c>Atlas.SharedKernel</c>:
/// for <c>es-ES</c> the chain is <c>["es-ES", "es", "en"]</c>.
/// This means:
/// <list type="number">
///   <item>Try <c>es-ES</c> (exact locale).</item>
///   <item>Try <c>es</c>   (language-only).</item>
///   <item>Try <c>en</c>   (ultimate English fallback).</item>
/// </list>
/// </para>
/// </summary>
public interface ITranslationLookupService
{
    /// <summary>
    /// Returns the resolved text and the language code that was actually used
    /// (which may differ from the requested language when a fallback was applied).
    /// Returns <see cref="Error.NotFound"/> only when no entry exists in any fallback language.
    /// </summary>
    Task<Result<(string Text, string ActualLanguage, bool IsFallback)>> ResolveAsync(TranslationCode code,
        LanguageCode requestedLanguage, CancellationToken ct = default);
}
