using ATLAS.Kernel.i18n.Domain.Interfaces.Services;
using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;

namespace ATLAS.Kernel.i18n.Domain.Services;

/// <summary>
/// Provides translation lookup services using a specified translation repository.
/// </summary>
/// <remarks>This class implements translation resolution with support for language fallback chains. It is
/// intended to be used as a singleton or scoped service within an application's dependency injection container. Thread
/// safety depends on the underlying repository implementation.</remarks>
/// <remarks>
/// 
/// </remarks>
/// <param name="repository"></param>
public sealed class TranslationLookupService(ITranslationRepository repository) : ITranslationLookupService
{

    /// <inheritdoc />
    public async Task<Result<(string Text, string ActualLanguage, bool IsFallback)>> ResolveAsync(TranslationCode code,
        LanguageCode requestedLanguage, CancellationToken ct = default)
    {
        // Walk the fallback chain: ["es-ES", "es", "en"]
        foreach (var tag in requestedLanguage.FallbackChain)
        {
            var langResult = LanguageCode.Create(tag);
            if (langResult.IsFailure) continue;

            var translation = await repository.FindAsync(code, langResult.Value, ct);
            if (translation is not null)
            {
                var isFallback = tag != requestedLanguage.Value;
                return (translation.Text, tag, isFallback);
            }
        }

        return Error.NotFound(
            "Translation.NotFound",
            $"No translation found for code '{code.Value}' in language '{requestedLanguage.Value}' " +
            $"or any fallback language.");
    }
}
