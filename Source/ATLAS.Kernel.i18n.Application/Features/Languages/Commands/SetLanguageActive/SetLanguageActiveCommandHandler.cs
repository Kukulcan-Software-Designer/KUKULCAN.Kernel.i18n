using ATLAS.Kernel.i18n.Domain.Interfaces.Repositories;

namespace ATLAS.Kernel.i18n.Application.Features.Languages.Commands.SetLanguageActive;

/// <summary>
/// Handles commands to activate or deactivate a language in the system.
/// </summary>
/// <remarks>This handler updates the active status of a language based on the provided command and ensures that
/// related cache entries are invalidated after the operation. It relies on the language repository for data access, a
/// unit of work for transactional consistency, and a cache service for cache management. The handler returns a result
/// indicating the outcome of the operation, including error information if the language is not found or if deactivation
/// is not allowed due to business rules.</remarks>
public sealed class SetLanguageActiveCommandHandler(ILanguageRepository repository, IUnitOfWork unitOfWork, ICacheService cache) : IRequestHandler<SetLanguageActiveCommand, Result>
{
    /// <summary>
    /// Handles the activation or deactivation of a language based on the specified command.
    /// </summary>
    /// <remarks>If the language is deactivated and it is the default language, the operation will fail with a
    /// conflict result. The method also updates the language cache to reflect the changes.</remarks>
    /// <param name="request">The command containing the language code and the desired active state. Cannot be null.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result indicating the outcome of the operation. Returns an error result if the language is not found or if
    /// deactivation is not allowed.</returns>
    public async Task<Result> Handle(SetLanguageActiveCommand request, CancellationToken cancellationToken)
    {
        var language = await repository.GetByCodeAsync(request.Code, cancellationToken);
        if (language is null)
            return Error.NotFound("Language.NotFound", $"Language '{request.Code}' was not found.");

        Result opResult;
        if (request.IsActive)
        {
            language.Activate();
            opResult = Result.Ok();
        }
        else
        {
            opResult = language.Deactivate(); // returns Conflict if IsDefault
        }

        if (opResult.IsFailure) return opResult.Error;

        repository.Update(language);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync(I18nCacheKeys.Language(request.Code), cancellationToken);
        await cache.RemoveAsync(I18nCacheKeys.LanguagesAll, cancellationToken);
        await cache.RemoveAsync(I18nCacheKeys.LanguagesActive, cancellationToken);

        return Result.Ok();
    }
}

