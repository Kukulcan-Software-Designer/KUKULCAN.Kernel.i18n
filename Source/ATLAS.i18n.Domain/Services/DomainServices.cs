namespace ATLAS.i18n.Domain.Services;

// ─── Translation lookup ───────────────────────────────────────────────────────

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
    Task<Result<(string Text, string ActualLanguage, bool IsFallback)>> ResolveAsync(
        TranslationCode   code,
        LanguageCode      requestedLanguage,
        CancellationToken ct = default);
}

/// <inheritdoc />
public sealed class TranslationLookupService : ITranslationLookupService
{
    private readonly ITranslationRepository _repository;

    public TranslationLookupService(ITranslationRepository repository)
        => _repository = repository;

    /// <inheritdoc />
    public async Task<Result<(string Text, string ActualLanguage, bool IsFallback)>> ResolveAsync(
        TranslationCode   code,
        LanguageCode      requestedLanguage,
        CancellationToken ct = default)
    {
        // Walk the fallback chain: ["es-ES", "es", "en"]
        foreach (var tag in requestedLanguage.FallbackChain)
        {
            var langResult = LanguageCode.Create(tag);
            if (langResult.IsFailure) continue;

            var translation = await _repository.FindAsync(code, langResult.Value, ct);
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

// ─── Default language management ─────────────────────────────────────────────

/// <summary>
/// Transfers the platform-default designation from the current default language
/// to a new one, ensuring exactly one language is always the default.
/// </summary>
public interface ILanguageDomainService
{
    /// <summary>
    /// Sets <paramref name="newDefaultCode"/> as the platform default.
    /// Unsets the previous default.
    /// Returns <see cref="Error.NotFound"/> when <paramref name="newDefaultCode"/> does not exist,
    /// or <see cref="Error.Conflict"/> when the language is inactive.
    /// </summary>
    Task<Result> SetDefaultLanguageAsync(
        string            newDefaultCode,
        CancellationToken ct = default);
}

/// <inheritdoc />
public sealed class LanguageDomainService : ILanguageDomainService
{
    private readonly ILanguageRepository _repository;

    public LanguageDomainService(ILanguageRepository repository)
        => _repository = repository;

    /// <inheritdoc />
    public async Task<Result> SetDefaultLanguageAsync(
        string            newDefaultCode,
        CancellationToken ct = default)
    {
        var newDefault = await _repository.GetByCodeAsync(newDefaultCode, ct);
        if (newDefault is null)
            return Error.NotFound(
                "Language.NotFound",
                $"Language '{newDefaultCode}' was not found.");

        if (!newDefault.IsActive)
            return Error.Conflict(
                "Language.Inactive",
                $"Language '{newDefaultCode}' is inactive. Activate it before setting it as default.");

        // Unset current default
        var currentDefault = await _repository.GetDefaultAsync(ct);
        if (currentDefault is not null && currentDefault.Id != newDefault.Id)
            currentDefault.UnmarkDefault();

        newDefault.MarkAsDefault();
        return Result.Ok();
    }
}
