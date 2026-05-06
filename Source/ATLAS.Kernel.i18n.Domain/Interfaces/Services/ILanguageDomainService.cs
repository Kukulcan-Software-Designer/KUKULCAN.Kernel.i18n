namespace ATLAS.Kernel.i18n.Domain.Interfaces.Services;

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
    Task<Result> SetDefaultLanguageAsync(string newDefaultCode, CancellationToken ct = default);
}
