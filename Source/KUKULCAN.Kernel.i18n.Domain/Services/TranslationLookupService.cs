using KUKULCAN.Kernel.i18n.Domain.Interfaces.Services;

namespace KUKULCAN.Kernel.i18n.Domain.Services;

/// <summary>
/// Represents the TranslationLookupService type.
/// </summary>
/// <param name="repository">The repository parameter.</param>
public sealed class TranslationLookupService(ITranslationRepository repository) : ITranslationLookupService
{
    /// <summary>
    /// Executes ResolveAsync.
    /// </summary>
    /// <param name="code">The code parameter.</param>
    /// <param name="requestedLanguage">The requestedLanguage parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The operation result.</returns>
    public async Task<Result<(string Text, string ActualLanguage, bool IsFallback)>> ResolveAsync(TranslationCode code, LanguageCode requestedLanguage, CancellationToken ct = default)
    {
        // Walk the fallback chain: ["es-ES", "es", "en"]
        foreach (var tag in requestedLanguage.FallbackChain)
        {
            Result<LanguageCode> langResult = LanguageCode.Create(tag);
            if (langResult.IsFailure)
                continue;

            Translation? translation = await repository.FindAsync(code, langResult.Value, ct);
            if (translation is null)
            {
                continue;
            }

            bool isFallback = tag != requestedLanguage.Value;
            return (translation.Text, tag, isFallback);
        }

        return Error.NotFound("Translation.NotFound", $"No translation found for code '{code.Value}' in language '{requestedLanguage.Value}' or any fallback language.");
    }
}
