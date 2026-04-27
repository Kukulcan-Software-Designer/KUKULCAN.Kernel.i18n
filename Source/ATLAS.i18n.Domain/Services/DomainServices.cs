using ATLAS.i18n.Domain.Entities;
using ATLAS.i18n.Domain.Exceptions;
using ATLAS.i18n.Domain.Repositories;
using ATLAS.i18n.Domain.ValueObjects;

namespace ATLAS.i18n.Domain.Services;

/// <summary>
/// Domain service handling language-level operations that span multiple aggregates
/// or require invariant enforcement beyond a single entity.
/// </summary>
public interface ILanguageDomainService
{
    /// <summary>
    /// Transfers the "default language" designation from the current default to the
    /// specified language. Ensures exactly one language is always the default.
    /// </summary>
    Task SetDefaultLanguageAsync(
        LanguageCode newDefault,
        CancellationToken ct = default);
}

/// <inheritdoc />
public sealed class LanguageDomainService : ILanguageDomainService
{
    private readonly ILanguageRepository _languageRepository;

    public LanguageDomainService(ILanguageRepository languageRepository)
        => _languageRepository = languageRepository;

    public async Task SetDefaultLanguageAsync(
        LanguageCode newDefault,
        CancellationToken ct = default)
    {
        var newDefaultLang = await _languageRepository.GetByCodeAsync(newDefault, ct)
            ?? throw new LanguageNotFoundException(newDefault.Value);

        if (!newDefaultLang.IsActive)
            throw new I18nDomainException(
                $"Cannot set inactive language '{newDefault.Value}' as the default.");

        var currentDefault = await _languageRepository.GetDefaultAsync(ct);

        if (currentDefault is not null && currentDefault.Id != newDefault)
            currentDefault.UnsetDefault();

        newDefaultLang.SetAsDefault();
    }
}

/// <summary>
/// Domain service for translation-level invariants that span multiple translations.
/// </summary>
public interface ITranslationDomainService
{
    /// <summary>
    /// Returns the text for the given code in the requested language.
    /// Falls back to English if the requested language is not available.
    /// Throws if even the English fallback is missing.
    /// </summary>
    Task<string> ResolveTextAsync(
        TranslationCode code,
        LanguageCode requestedLanguage,
        CancellationToken ct = default);
}

/// <inheritdoc />
public sealed class TranslationDomainService : ITranslationDomainService
{
    private readonly ITranslationRepository _translationRepository;

    public TranslationDomainService(ITranslationRepository translationRepository)
        => _translationRepository = translationRepository;

    public async Task<string> ResolveTextAsync(
        TranslationCode code,
        LanguageCode requestedLanguage,
        CancellationToken ct = default)
    {
        // Try the requested language first
        var translation = await _translationRepository.FindAsync(code, requestedLanguage, ct);

        if (translation is not null)
            return translation.Text;

        // Fall back to English (default)
        if (requestedLanguage != LanguageCode.English)
        {
            var fallback = await _translationRepository.FindAsync(
                code, LanguageCode.English, ct);

            if (fallback is not null)
                return fallback.Text;
        }

        throw new TranslationNotFoundException(code.Value, requestedLanguage.Value);
    }
}
