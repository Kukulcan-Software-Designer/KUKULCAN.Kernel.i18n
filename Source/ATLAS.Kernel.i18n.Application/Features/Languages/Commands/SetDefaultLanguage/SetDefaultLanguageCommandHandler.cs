using ATLAS.Kernel.i18n.Domain.Interfaces.Services;

namespace ATLAS.Kernel.i18n.Application.Features.Languages.Commands.SetDefaultLanguage;

/// <summary>
/// Represents the SetDefaultLanguageCommandHandler type.
/// </summary>
/// <remarks>
/// Initializes a new instance of the SetDefaultLanguageCommandHandler class with the specified domain service, unit
/// of work, and cache service.
/// </remarks>
/// <param name="domainService">The domain service used to manage language-related operations. Cannot be null.</param>
/// <param name="unitOfWork">The unit of work instance that manages transactional operations. Cannot be null.</param>
/// <param name="cache">The cache service used to store and retrieve language data. Cannot be null.</param>
public sealed class SetDefaultLanguageCommandHandler(ILanguageDomainService domainService, IUnitOfWork unitOfWork, ICacheService cache) :
    IRequestHandler<SetDefaultLanguageCommand, Result>
{
    /// <summary>
    /// Handles the setting of a default language.
    /// </summary>
    /// <param name="request">The command containing the details of the language to set as default.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<Result> Handle(SetDefaultLanguageCommand request, CancellationToken cancellationToken)
    {
        var result = await domainService.SetDefaultLanguageAsync(request.Code, cancellationToken);
        if (result.IsFailure)
            return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(I18NCacheKeys.LanguageDefault, cancellationToken);
        await cache.RemoveAsync(I18NCacheKeys.LanguagesAll, cancellationToken);
        await cache.RemoveAsync(I18NCacheKeys.LanguagesActive, cancellationToken);

        return Result.Ok();
    }
}
