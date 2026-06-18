using KUKULCAN.Kernel.i18n.Domain.Interfaces.Services;

namespace KUKULCAN.Kernel.i18n.Domain.Services;

/// <summary>
/// Provides domain-level operations for managing languages, including setting the default language.
/// </summary>
/// <remarks>This service coordinates language-related business logic and interacts with the underlying language
/// repository. It is intended to be used as the main entry point for language management within the domain layer.
/// Instances of this class are immutable and thread-safe.</remarks>
/// <remarks>
///
/// </remarks>
/// <param name="repository">The repository parameter.</param>
public sealed class LanguageDomainService(ILanguageRepository repository) : ILanguageDomainService
{
    /// <summary>
    /// Sets <paramref name="newDefaultCode"/> as the platform default.
    /// Unsets the previous default.
    /// Returns <see cref="Error.NotFound"/> when <paramref name="newDefaultCode"/> does not exist,
    /// or <see cref="Error.Conflict"/> when the language is inactive.
    /// </summary>
    /// <param name="newDefaultCode">The code of the new default language.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating the success or failure of the operation.</returns>
    public async Task<Result> SetDefaultLanguageAsync(string newDefaultCode, CancellationToken ct = default)
    {
        var newDefault = await repository.GetByCodeAsync(newDefaultCode, ct);
        if (newDefault is null)
            return Error.NotFound("Language.NotFound", $"Language '{newDefaultCode}' was not found.");

        if (!newDefault.IsActive)
            return Error.Conflict("Language.Inactive", $"Language '{newDefaultCode}' is inactive. Activate it before setting it as default.");

        // Unset current default
        var currentDefault = await repository.GetDefaultAsync(ct);
        if (currentDefault is not null && currentDefault.Id != newDefault.Id)
            currentDefault.UnmarkDefault();

        newDefault.MarkAsDefault();
        return Result.Ok();
    }
}
