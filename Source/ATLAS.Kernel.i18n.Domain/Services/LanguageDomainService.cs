using ATLAS.Kernel.i18n.Domain.Interfaces.Services;
using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;

namespace ATLAS.Kernel.i18n.Domain.Services;

/// <summary>
/// Provides domain-level operations for managing languages, including setting the default language.
/// </summary>
/// <remarks>This service coordinates language-related business logic and interacts with the underlying language
/// repository. It is intended to be used as the main entry point for language management within the domain layer.
/// Instances of this class are immutable and thread-safe.</remarks>
/// <remarks>
/// 
/// </remarks>
/// <param name="repository"></param>
public sealed class LanguageDomainService(ILanguageRepository repository) : ILanguageDomainService
{

    /// <inheritdoc />
    public async Task<Result> SetDefaultLanguageAsync(string newDefaultCode, CancellationToken ct = default)
    {
        var newDefault = await repository.GetByCodeAsync(newDefaultCode, ct);
        if (newDefault is null)
            return Error.NotFound(
                "Language.NotFound",
                $"Language '{newDefaultCode}' was not found.");

        if (!newDefault.IsActive)
            return Error.Conflict(
                "Language.Inactive",
                $"Language '{newDefaultCode}' is inactive. Activate it before setting it as default.");

        // Unset current default
        var currentDefault = await repository.GetDefaultAsync(ct);
        if (currentDefault is not null && currentDefault.Id != newDefault.Id)
            currentDefault.UnmarkDefault();

        newDefault.MarkAsDefault();
        return Result.Ok();
    }
}
