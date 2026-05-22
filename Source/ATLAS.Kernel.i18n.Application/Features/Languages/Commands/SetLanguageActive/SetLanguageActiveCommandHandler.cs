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
public sealed class SetLanguageActiveCommandHandler(ILanguageRepository repository, IUnitOfWork unitOfWork, ICacheService cache) :
    IRequestHandler<SetLanguageActiveCommand, Result>
{
    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="request">The request parameter.</param>
    /// <param name="cancellationToken">The cancellationToken parameter.</param>
    /// <returns>The operation result.</returns>
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
        if (opResult.IsFailure)
            return opResult.Error;
        repository.Update(language);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(I18NCacheKeys.Language(request.Code), cancellationToken);
        await cache.RemoveAsync(I18NCacheKeys.LanguagesAll, cancellationToken);
        await cache.RemoveAsync(I18NCacheKeys.LanguagesActive, cancellationToken);

        return Result.Ok();
    }
}

