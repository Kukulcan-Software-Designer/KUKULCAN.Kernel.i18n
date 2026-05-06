using ATLAS.Kernel.i18n.Domain.Interfaces.Services;

namespace ATLAS.Kernel.i18n.Application.Features.Languages.Commands.SetDefaultLanguage;

/// <summary>
/// 
/// </summary>
/// <remarks>
/// Initializes a new instance of the SetDefaultLanguageCommandHandler class with the specified domain service, unit
/// of work, and cache service.
/// </remarks>
/// <param name="domainService">The domain service used to manage language-related operations. Cannot be null.</param>
/// <param name="unitOfWork">The unit of work instance that manages transactional operations. Cannot be null.</param>
/// <param name="cache">The cache service used to store and retrieve language data. Cannot be null.</param>
public sealed class SetDefaultLanguageCommandHandler(ILanguageDomainService domainService, IUnitOfWork unitOfWork, ICacheService cache) : IRequestHandler<SetDefaultLanguageCommand, Result>
{
    /// <summary>
    /// Handles the command to set the default language for the application.
    /// </summary>
    /// <remarks>Removes related language cache entries after successfully setting the default language to
    /// ensure subsequent queries reflect the updated state.</remarks>
    /// <param name="request">The command containing the language code to set as the default. Cannot be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A result indicating whether the operation succeeded or failed. Returns an error result if the language could not
    /// be set.</returns>
    public async Task<Result> Handle(SetDefaultLanguageCommand request, CancellationToken cancellationToken)
    {
        var result = await domainService.SetDefaultLanguageAsync(request.Code, cancellationToken);
        if (result.IsFailure) return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync(I18nCacheKeys.LanguageDefault, cancellationToken);
        await cache.RemoveAsync(I18nCacheKeys.LanguagesAll, cancellationToken);
        await cache.RemoveAsync(I18nCacheKeys.LanguagesActive, cancellationToken);

        return Result.Ok();
    }
}
